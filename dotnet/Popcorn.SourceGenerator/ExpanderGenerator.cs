﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

#nullable enable
namespace Popcorn.SourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class ExpanderGenerator : IIncrementalGenerator
    {
        private const string JsonSerializableAttributeTypeName = "System.Text.Json.Serialization.JsonSerializableAttribute";
        private const string JsonSerializerContextTypeName = "System.Text.Json.Serialization.JsonSerializerContext";
        private const string IEnumerableTypeName = "System.Collections.Generic.IEnumerable<T>";
        // NOTE: whitespace matters. Roslyn's ToDisplayString emits generic argument lists with
        // ", " (comma + space). If the constant uses a no-space form, InheritsOrImplements will
        // never match — Dictionary<K,V> happens to fall through a separate Dictionary<TKey,TValue>
        // OriginalDefinition check in GenerateJsonConverter, but IDictionary<K,V> and
        // ReadOnlyDictionary<K,V> as the *target* type have no such fallback, so they silently
        // hit the IEnumerable branch and emit broken iterators (treating KeyValuePair<K,V> as K).
        // Fixed as part of Bug 5.
        private const string IDictionaryTypeName = "System.Collections.Generic.IDictionary<TKey, TValue>";
        private static readonly HashSet<string> NumberTypes = new HashSet<string>([
            typeof(Decimal).FullName,
            typeof(Byte).FullName,
            typeof(UInt16).FullName,
            typeof(UInt32).FullName,
            typeof(UInt64).FullName,
            typeof(SByte).FullName,
            typeof(Int16).FullName,
            typeof(Int32).FullName,
            typeof(Int64).FullName,
            "decimal",
            "byte",
            "sbyte",
            "short",
            "ushort",
            "int",
            "uint",
            "long",
            "ulong",
        ]);

        private static readonly HashSet<string> StringTypes = new HashSet<string>([
            "string",
            typeof(string).FullName,
            typeof(Span<char>).FullName,
            typeof(ReadOnlySpan<char>).FullName,
            typeof(Memory<char>).FullName,
            typeof(ReadOnlyMemory<char>).FullName
        ]);

        private static readonly HashSet<string> BoolTypes = new HashSet<string>([
            "bool",
            typeof(bool).FullName,
        ]);

        private static readonly HashSet<string> IgnoreTypes = new HashSet<string>([
            "char",
            typeof(Single).FullName,
            typeof(Double).FullName,
            "float",
            "double",
            typeof(char).FullName,
            typeof(Guid).FullName,
            typeof(DateTime).FullName,
            typeof(TimeSpan).FullName,
            typeof(DateTimeOffset).FullName, // We need a way to say "opt out of expanding these"
        ]);

        // True when the type is a primitive / ignored / enum — one that we do NOT emit a Pop<T>
        // body for and must NOT have in `allTypeNames`. Value-type nullable wrappers (Nullable<int>)
        // are treated the same as their underlying primitive here: both collapse to "int" under the
        // Replace("?", "") convention used throughout the generator.
        private static bool IsBlindSerializableType(ITypeSymbol? type)
        {
            if (type == null) return false;
            if (type.TypeKind == TypeKind.Enum) return true;
            if (type is INamedTypeSymbol named
                && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                && named.TypeArguments.Length == 1
                && named.TypeArguments[0].TypeKind == TypeKind.Enum)
            {
                return true;
            }
            var name = type.ToDisplayString().Replace("?", "");
            return NumberTypes.Contains(name)
                || StringTypes.Contains(name)
                || BoolTypes.Contains(name)
                || IgnoreTypes.Contains(name);
        }

        // Unwrap Nullable<T>, arrays, IEnumerable<T>, IDictionary<K,V> → the inner element/value
        // type that a converter would recurse into. Non-collection named types pass through.
        private static ITypeSymbol? UnwrapPayloadType(ITypeSymbol? type)
        {
            if (type == null) return null;
            if (type is INamedTypeSymbol named
                && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                && named.TypeArguments.Length == 1)
            {
                return UnwrapPayloadType(named.TypeArguments[0]);
            }
            if (type is IArrayTypeSymbol array)
            {
                return UnwrapPayloadType(array.ElementType);
            }
            if (type is INamedTypeSymbol namedDict && InheritsOrImplements(namedDict, IDictionaryTypeName)
                && namedDict.TypeArguments.Length >= 2)
            {
                return UnwrapPayloadType(namedDict.TypeArguments[1]);
            }
            if (type is INamedTypeSymbol namedEnum && InheritsOrImplements(namedEnum, IEnumerableTypeName)
                && namedEnum.TypeArguments.Length >= 1)
            {
                return UnwrapPayloadType(namedEnum.TypeArguments[0]);
            }
            return type;
        }

        // True if the payload type's transitive property graph (limited to types Popcorn itself
        // recurses into — i.e. members of `allTypeNames`) never contains a path back to itself.
        // A cycle-safe converter can skip the per-call HashSet<object> allocation used for
        // circular-reference detection.
        private static bool IsConverterCycleSafe(ITypeSymbol rootType, HashSet<string> allTypeNames)
        {
            var payload = UnwrapPayloadType(rootType);
            if (payload is not INamedTypeSymbol named) return true;
            if (IsBlindSerializableType(named)) return true;
            return IsNamedTypeCycleSafe(named, allTypeNames, new HashSet<string>());
        }

        private static bool IsNamedTypeCycleSafe(INamedTypeSymbol type, HashSet<string> allTypeNames, HashSet<string> onPath)
        {
            var typeName = type.ToDisplayString().Replace("?", "");
            // Types Popcorn doesn't have a Pop<T> body for (primitives, enums, unregistered externals)
            // don't participate in Popcorn's recursion — the visited HashSet is never consulted for them.
            if (IsBlindSerializableType(type)) return true;
            if (!allTypeNames.Contains(typeName)) return true;
            if (!onPath.Add(typeName)) return false; // reached an ancestor of ourselves → cycle risk

            try
            {
                foreach (var prop in GetSerializableProperties(type))
                {
                    if (!ShouldSerializeMember(prop)) continue;
                    var payload = UnwrapPayloadType(prop.Type);
                    if (payload is INamedTypeSymbol p && !IsNamedTypeCycleSafe(p, allTypeNames, onPath))
                        return false;
                }
                foreach (var field in GetSerializableFields(type))
                {
                    if (!ShouldSerializeMember(field)) continue;
                    var payload = UnwrapPayloadType(field.Type);
                    if (payload is INamedTypeSymbol f && !IsNamedTypeCycleSafe(f, allTypeNames, onPath))
                        return false;
                }
                return true;
            }
            finally
            {
                onPath.Remove(typeName);
            }
        }

        // Emit type arguments for Pop<...> without NRT annotations on reference types. Preserves
        // `Nullable<T>` on value types (a distinct CLR type). Registered converter method
        // signatures and every Pop<...> callsite use this same formatter so they never diverge by
        // `?` annotation — which was the root cause of the CS8620 warnings (Bug 3).
        //
        // SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier affects ONLY
        // reference-type `?` annotations. `Nullable<int>` still renders as `int?` under
        // `UseSpecialTypes` regardless of that flag — because `int?` there is syntactic sugar for
        // the real CLR type `System.Nullable<Int32>`, not an NRT annotation.
        private static readonly SymbolDisplayFormat PopTypeArgumentFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static string TypeNameForPop(ITypeSymbol type)
            => type.ToDisplayString(PopTypeArgumentFormat);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Step 1: Find all classes that inherit from JsonSerializerContext
            var jsonSerializerContextClasses = context.SyntaxProvider
                .ForAttributeWithMetadataName(JsonSerializableAttributeTypeName,
                    predicate: (node, _) => node is ClassDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0,
                    transform: (ctx, _) => GetJsonSerializerContextClass(ctx))
                .Where(symbol => symbol != null);

            // Step 2: Extract [JsonSerializable] attributes and their target types
            var jsonSerializableAttributes = jsonSerializerContextClasses
                .Select((classSymbol, _) => new GeneratorClassReference(classSymbol, GetJsonSerializableTypes(classSymbol!)))
                .Where(data => data.Attributes.Any());


            // Step 3: Devolve to the actual types referenced.
            // Step 3: Generate the JsonConverter class for each target type
            context.RegisterSourceOutput(jsonSerializableAttributes, (spc, data) =>
            {
                if (data.ClassSymbol == null)
                {
                    return;
                }

                var targetTypes = new HashSet<ITypeSymbol>(
                    data.Attributes
                        .Select(attribute => attribute.ConstructorArguments[0].Value as INamedTypeSymbol)
                        .Where(a => a != null && a.TypeArguments.Length > 0)
                        .Select(a => a?.TypeArguments[0] as ITypeSymbol)
                        .Where(a => a != null)!,
                    SymbolEqualityComparer.Default);

                foreach (var targetType in targetTypes.ToList())
                {
                    foreach (var t in GetReferencedTypes(targetType, data.ClassSymbol, spc))
                    {
                        targetTypes.Add(t);
                    }
                }

                foreach (var targetType in targetTypes)
                {
                    try
                    {
                        var source = GenerateJsonConverter(targetType, data.ClassSymbol, targetTypes, spc);
                        spc.AddSource($"{NameType(targetType)}JsonConverter.g.cs", SourceText.From(source, Encoding.UTF8));
                    }
                    catch (Exception ex)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                id: "JSG001",
                                title: "Source Generation Error",
                                messageFormat: $"Error generating source for type '{targetType}': {ex.Message}",
                                category: "SourceGenerator",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true),
                            Location.None));
                    }
                }


                try
                {
                    // Collect custom envelope open-generic definitions from the JsonSerializable attrs.
                    var customEnvelopes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                    foreach (var attr in data.Attributes)
                    {
                        if (attr.ConstructorArguments[0].Value is INamedTypeSymbol envType
                            && HasPopcornEnvelopeAttribute(envType))
                        {
                            customEnvelopes.Add(envType.OriginalDefinition);
                        }
                    }

                    var errorWriterBody = new StringBuilder();
                    foreach (var envelope in customEnvelopes)
                    {
                        var analysis = AnalyzeEnvelope(envelope);
                        ReportEnvelopeDiagnostics(spc, envelope, analysis);
                        if (analysis.PayloadName == null)
                        {
                            // Missing payload already reported as JSG003; skip emission for this envelope.
                            continue;
                        }
                        if (HasGenericContainingType(envelope))
                        {
                            // Nested inside a generic outer — open-generic typeof syntax can't be expressed.
                            // Reported as JSG007; skip emission.
                            continue;
                        }
                        errorWriterBody.Append($@"
            if (envelopeType == typeof({OpenGenericCSharpName(envelope)}))
            {{
                writer.WriteStartObject();
                {(analysis.SuccessName != null ? $@"writer.WriteBoolean(Convert(""{analysis.SuccessName}""), false);" : "")}
                {(analysis.ErrorName != null ? $@"writer.WriteStartObject(Convert(""{analysis.ErrorName}""));
                writer.WriteString(Convert(""Code""), error.Code);
                writer.WriteString(Convert(""Message""), error.Message);
                if (error.Detail != null) writer.WriteString(Convert(""Detail""), error.Detail);
                writer.WriteEndObject();" : "")}
                writer.WriteEndObject();
                return;
            }}");
                    }

                    var envelopeRegistrationCall = customEnvelopes.Count == 0
                        ? ""
                        : $@"
        global::Popcorn.Shared.PopcornErrorWriterRegistry.Register(WriteCustomErrorEnvelope);";

                    var envelopeWriterMethod = customEnvelopes.Count == 0 || errorWriterBody.Length == 0
                        ? ""
                        : $@"

    private static void WriteCustomErrorEnvelope(
        global::System.Text.Json.Utf8JsonWriter writer,
        global::System.Type envelopeType,
        global::Popcorn.Shared.ApiError error,
        global::System.Text.Json.JsonNamingPolicy? namingPolicy)
    {{
        string Convert(string name) => namingPolicy?.ConvertName(name) ?? name;{errorWriterBody}
    }}";

                    // Now add the top-level extension method for registering all our converters to the WebApi pipeline.
                    // Both AddPopcornOptions (JsonSerializerOptions-level) and AddPopcornEnvelopes (IServiceCollection-level)
                    // register the error-envelope writer. AddPopcornEnvelopes is the AOT-friendly DI-time hook; AddPopcornOptions
                    // remains the JSON-level hook that was added alongside the converter registration.
                    spc.AddSource("RegisterConverters.g.cs", SourceText.From($@"// <auto-generated/>
#nullable enable
namespace Popcorn.Shared;

public static class PopcornJsonOptionsExtension
{{
    public static void AddPopcornOptions(this global::System.Text.Json.JsonSerializerOptions options)
    {{
        options.NumberHandling = global::System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
        {String.Join("", targetTypes.Select(targetType => $@"
        options.Converters.Add(new global::Popcorn.Generated.Converters.{NameType(targetType)}JsonConverter());")
            )}{envelopeRegistrationCall}
    }}

    public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddPopcornEnvelopes(
        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {{{envelopeRegistrationCall}
        return services;
    }}{envelopeWriterMethod}
}}
", Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "JSG001",
                            title: "Source Generation Error",
                            messageFormat: $"Error generating registration source': {ex.Message}",
                            category: "SourceGenerator",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None));
                }

            });
        }

        private static INamedTypeSymbol? GetJsonSerializerContextClass(GeneratorAttributeSyntaxContext context)
        {
            var classSymbol = context.TargetSymbol as INamedTypeSymbol;

            // Check if the class is a subclass of JsonSerializerContext
            var baseType = classSymbol?.BaseType;
            var typeName = baseType?.ToDisplayString();
            return typeName == JsonSerializerContextTypeName ? classSymbol : null;
        }

        private static IEnumerable<AttributeData> GetJsonSerializableTypes(INamedTypeSymbol classSymbol)
        {
            return classSymbol.GetAttributes()
                .Where(attr => attr.AttributeClass?.ToDisplayString() == JsonSerializableAttributeTypeName &&
                    attr.ConstructorArguments.Length > 0
                    && attr.ConstructorArguments[0].Value is INamedTypeSymbol typeSymbol
                    && (InheritsOrImplements(typeSymbol, "Popcorn.Shared.ApiResponse<T>")
                        || HasPopcornEnvelopeAttribute(typeSymbol)));
        }

        private static bool HasPopcornEnvelopeAttribute(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString() == "Popcorn.PopcornEnvelopeAttribute");
        }

        private class EnvelopeAnalysis
        {
            public string? SuccessName;
            public string? PayloadName;
            public string? ErrorName;
            public ITypeSymbol? PayloadType;
            public ITypeSymbol? ErrorType;
            public Location? EnvelopeLocation;
            public Location? PayloadLocation;
            public Location? ErrorLocation;
            public List<Location> DuplicateSuccessLocations = new List<Location>();
            public List<Location> DuplicatePayloadLocations = new List<Location>();
            public List<Location> DuplicateErrorLocations = new List<Location>();
        }

        private static EnvelopeAnalysis AnalyzeEnvelope(INamedTypeSymbol envelope)
        {
            var analysis = new EnvelopeAnalysis
            {
                EnvelopeLocation = envelope.Locations.FirstOrDefault(),
            };

            // Walk the base chain so markers on a base envelope class are honored.
            foreach (var prop in GetSerializableProperties(envelope))
            {
                var wireName = GetJsonPropertyNameOverride(prop) ?? prop.Name;
                foreach (var attr in prop.GetAttributes())
                {
                    var attrName = attr.AttributeClass?.ToDisplayString();
                    var location = prop.Locations.FirstOrDefault();
                    if (attrName == "Popcorn.PopcornSuccessAttribute")
                    {
                        if (analysis.SuccessName != null) analysis.DuplicateSuccessLocations.Add(location ?? Location.None);
                        analysis.SuccessName = wireName;
                    }
                    else if (attrName == "Popcorn.PopcornPayloadAttribute")
                    {
                        if (analysis.PayloadName != null) analysis.DuplicatePayloadLocations.Add(location ?? Location.None);
                        analysis.PayloadName = wireName;
                        analysis.PayloadType = prop.Type;
                        analysis.PayloadLocation = location;
                    }
                    else if (attrName == "Popcorn.PopcornErrorAttribute")
                    {
                        if (analysis.ErrorName != null) analysis.DuplicateErrorLocations.Add(location ?? Location.None);
                        analysis.ErrorName = wireName;
                        analysis.ErrorType = prop.Type;
                        analysis.ErrorLocation = location;
                    }
                }
            }
            return analysis;
        }

        private static string? GetJsonPropertyNameOverride(IPropertySymbol prop)
        {
            var attr = prop.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString() == "System.Text.Json.Serialization.JsonPropertyNameAttribute");
            if (attr?.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
            {
                return name;
            }
            return null;
        }

        private static bool IsPopOfT(ITypeSymbol? type)
        {
            if (type is not INamedTypeSymbol named) return false;
            return named.OriginalDefinition.ToDisplayString() == "Popcorn.Shared.Pop<T>";
        }

        private static bool IsApiError(ITypeSymbol? type)
        {
            if (type is null) return false;
            // Accept ApiError and ApiError? (Nullable<T> for value types, or annotated reference)
            if (type is INamedTypeSymbol nullable
                && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                && nullable.TypeArguments.Length == 1)
            {
                type = nullable.TypeArguments[0];
            }
            return type.ToDisplayString().TrimEnd('?') == "Popcorn.Shared.ApiError";
        }

        /// <summary>
        /// Emits the C# open-generic syntax for <c>typeof(...)</c>, walking the containing-type chain so
        /// nested types render as <c>Outer.Inner&lt;&gt;</c>. Generic outer types are not supported and
        /// will produce code that does not compile — flagged by diagnostic JSG007.
        /// </summary>
        private static string OpenGenericCSharpName(INamedTypeSymbol type)
        {
            var parts = new List<string>();
            var cursor = (INamedTypeSymbol?)type;
            while (cursor != null)
            {
                var name = cursor.Name;
                if (cursor.Arity == 0)
                {
                    parts.Insert(0, name);
                }
                else
                {
                    var commas = new string(',', cursor.Arity - 1);
                    parts.Insert(0, $"{name}<{commas}>");
                }
                cursor = cursor.ContainingType;
            }

            var ns = type.ContainingNamespace?.IsGlobalNamespace == true
                ? null
                : type.ContainingNamespace?.ToDisplayString();
            var joined = string.Join(".", parts);
            return string.IsNullOrEmpty(ns) ? $"global::{joined}" : $"global::{ns}.{joined}";
        }

        private static bool HasGenericContainingType(INamedTypeSymbol type)
        {
            for (var c = type.ContainingType; c != null; c = c.ContainingType)
            {
                if (c.Arity > 0) return true;
            }
            return false;
        }

        private static readonly DiagnosticDescriptor EnvelopeMissingPayloadDescriptor = new DiagnosticDescriptor(
            id: "JSG003",
            title: "Envelope missing [PopcornPayload]",
            messageFormat: "Envelope '{0}' is marked with [PopcornEnvelope] but has no [PopcornPayload] property. The exception middleware will fall back to the default ApiResponse shape for this envelope type.",
            category: "SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor EnvelopeDuplicateMarkerDescriptor = new DiagnosticDescriptor(
            id: "JSG004",
            title: "Envelope has duplicate marker",
            messageFormat: "Envelope '{0}' has multiple properties marked with [Popcorn{1}]. Only the last one is used.",
            category: "SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor EnvelopePayloadTypeDescriptor = new DiagnosticDescriptor(
            id: "JSG005",
            title: "Envelope [PopcornPayload] should be Pop<T>",
            messageFormat: "Property '{0}' on envelope '{1}' is marked with [PopcornPayload] but is typed as '{2}' instead of Pop<T>. Property-reference filtering will not be applied to this payload.",
            category: "SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor EnvelopeErrorTypeDescriptor = new DiagnosticDescriptor(
            id: "JSG006",
            title: "Envelope [PopcornError] should be ApiError",
            messageFormat: "Property '{0}' on envelope '{1}' is marked with [PopcornError] but is typed as '{2}' instead of ApiError or ApiError?. The exception middleware may produce a shape that does not round-trip to your envelope type.",
            category: "SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor EnvelopeGenericOuterDescriptor = new DiagnosticDescriptor(
            id: "JSG007",
            title: "Envelope nested in a generic outer type is not supported",
            messageFormat: "Envelope '{0}' is nested inside a generic outer type. The generator cannot emit the open-generic typeof expression required to dispatch error envelopes for this shape. Move the envelope to the top level or inside a non-generic container.",
            category: "SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static void ReportEnvelopeDiagnostics(SourceProductionContext spc, INamedTypeSymbol envelope, EnvelopeAnalysis analysis)
        {
            var envelopeName = envelope.ToDisplayString();
            var envelopeLocation = analysis.EnvelopeLocation ?? Location.None;

            if (analysis.PayloadName == null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(EnvelopeMissingPayloadDescriptor, envelopeLocation, envelopeName));
            }

            foreach (var loc in analysis.DuplicateSuccessLocations)
            {
                spc.ReportDiagnostic(Diagnostic.Create(EnvelopeDuplicateMarkerDescriptor, loc, envelopeName, "Success"));
            }
            foreach (var loc in analysis.DuplicatePayloadLocations)
            {
                spc.ReportDiagnostic(Diagnostic.Create(EnvelopeDuplicateMarkerDescriptor, loc, envelopeName, "Payload"));
            }
            foreach (var loc in analysis.DuplicateErrorLocations)
            {
                spc.ReportDiagnostic(Diagnostic.Create(EnvelopeDuplicateMarkerDescriptor, loc, envelopeName, "Error"));
            }

            if (analysis.PayloadType != null && !IsPopOfT(analysis.PayloadType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    EnvelopePayloadTypeDescriptor,
                    analysis.PayloadLocation ?? envelopeLocation,
                    analysis.PayloadName,
                    envelopeName,
                    analysis.PayloadType.ToDisplayString()));
            }

            if (analysis.ErrorType != null && !IsApiError(analysis.ErrorType))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    EnvelopeErrorTypeDescriptor,
                    analysis.ErrorLocation ?? envelopeLocation,
                    analysis.ErrorName,
                    envelopeName,
                    analysis.ErrorType.ToDisplayString()));
            }

            if (HasGenericContainingType(envelope))
            {
                spc.ReportDiagnostic(Diagnostic.Create(EnvelopeGenericOuterDescriptor, envelopeLocation, envelopeName));
            }
        }

        private static bool InheritsOrImplements(ITypeSymbol typeSymbol, string baseTypeName)
        {
            var visitedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            var typesToVisit = new Queue<ITypeSymbol>();
            typesToVisit.Enqueue(typeSymbol);

            while (typesToVisit.Any())
            {
                var cursorSymbol = typesToVisit.Dequeue();
                if (!visitedTypes.Add(cursorSymbol))
                {
                    continue;
                }

                if (cursorSymbol.OriginalDefinition.ToDisplayString() == baseTypeName)
                {
                    return true;
                }

                if (cursorSymbol.BaseType != null && !visitedTypes.Contains(cursorSymbol.BaseType))
                {
                    typesToVisit.Enqueue(cursorSymbol.BaseType);
                }
                foreach (var interfaceType in cursorSymbol.AllInterfaces)
                {
                    if (!visitedTypes.Contains(interfaceType))
                    {
                        typesToVisit.Enqueue(interfaceType);
                    }
                }
            }
            return false;
        }

        private static HashSet<ITypeSymbol> GetReferencedTypes(ITypeSymbol targetType, INamedTypeSymbol classSymbol, SourceProductionContext context)
        {
            // We need to build out the recursive references here
            // Visit each type and find each property that could be serialized and ensure that its type is added to the list.
            var visitedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            var typesToVisit = new Queue<ITypeSymbol>();
            typesToVisit.Enqueue(targetType);

            while (typesToVisit.Count > 0)
            {
                var currentType = typesToVisit.Dequeue();
                
                // Handle array types
                if (currentType is IArrayTypeSymbol arrayType)
                {
                    // Add the array type itself to visitedTypes
                    if (!visitedTypes.Add(arrayType))
                    {
                        continue;
                    }

                    if (arrayType.ElementType is INamedTypeSymbol elementNamedType)
                    {
                        var propertyTypeName = elementNamedType.ToDisplayString().Replace("?", "");
                        if (!IgnoreTypes.Contains(propertyTypeName) 
                            && !NumberTypes.Contains(propertyTypeName)
                            && !StringTypes.Contains(propertyTypeName)
                            && !BoolTypes.Contains(propertyTypeName))
                        {
                            if (!visitedTypes.Contains(elementNamedType))
                            {
                                typesToVisit.Enqueue(elementNamedType);
                            }
                        }
                    }
                    continue;
                }
                
                // Handle named types
                if (currentType is INamedTypeSymbol namedType)
                {
                    var propertyTypeName = namedType.ToDisplayString().Replace("?", "");
                    if (IgnoreTypes.Contains(propertyTypeName)
                        || NumberTypes.Contains(propertyTypeName)
                        || StringTypes.Contains(propertyTypeName)
                        || BoolTypes.Contains(propertyTypeName))
                    {
                        continue;
                    }

                    // Enums (and Nullable<Enum>) are handled by System.Text.Json directly.
                    // Skipping here prevents the generator from treating them as complex objects
                    // and keeps them out of allTypeNames, so the default JsonSerializer.Serialize
                    // fallback in AddMemberSerializationCode handles them correctly. This also
                    // means global options.Converters (e.g. JsonStringEnumConverter) and per-type
                    // [JsonConverter] attributes on the enum work transparently.
                    if (namedType.TypeKind == TypeKind.Enum)
                    {
                        continue;
                    }
                    if (namedType.OriginalDefinition?.SpecialType == SpecialType.System_Nullable_T &&
                        namedType.TypeArguments.Length == 1 &&
                        namedType.TypeArguments[0].TypeKind == TypeKind.Enum)
                    {
                        continue;
                    }

                    if (!visitedTypes.Add(namedType))
                    {
                        continue;
                    }

                    // Check if this is a collection type and extract its item type
                    if (namedType.OriginalDefinition != null)
                    {
                        // Handle "dictionary" types eg IDictionary<K,V> - only consider the value type V, not the key type K
                        if (InheritsOrImplements(namedType, IDictionaryTypeName) &&
                                namedType.TypeArguments.Length > 1)
                        {
                            var valueType = namedType.TypeArguments[1];
                            if (valueType is INamedTypeSymbol valueNamedType && !visitedTypes.Contains(valueNamedType))
                            {
                                typesToVisit.Enqueue(valueNamedType);
                            }
                            continue;
                        }

                        // Handle "list" types
                        else if (InheritsOrImplements(namedType, IEnumerableTypeName) &&
                            namedType.TypeArguments.Length > 0)
                        {
                            var itemType = namedType.TypeArguments[0];
                            if (itemType is INamedTypeSymbol itemNamedType && !visitedTypes.Contains(itemNamedType))
                            {
                                typesToVisit.Enqueue(itemNamedType);
                            }

                            continue;
                        }
                    }

                    // Walk inherited members too — GetMembers() only returns declared-on-this-type,
                    // but [Always]/[Default] on a base class must apply to derived types.
                    foreach (var property in GetSerializableProperties(namedType))
                    {
                        typesToVisit.Enqueue(property.Type);
                    }

                    foreach (var field in GetSerializableFields(namedType))
                    {
                        typesToVisit.Enqueue(field.Type);
                    }
                }
            }

            return visitedTypes;
        }

        // Check if a type is nullable
        private static bool IsNullableType(ITypeSymbol typeSymbol)
        {
            // Arrays are reference types and thus nullable
            if (typeSymbol is IArrayTypeSymbol)
            {
                return true;
            }
            
            // Handle named types
            if (typeSymbol is INamedTypeSymbol namedType)
            {
                // Case 1: Reference types are inherently nullable
                if (!namedType.IsValueType)
                {
                    return true;
                }
                
                // Case 2: Nullable value types (Nullable<T>)
                if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    return true;
                }
            }
            
            // Not nullable
            return false;
        }

        private static string GenerateJsonConverter(ITypeSymbol targetType, INamedTypeSymbol classSymbol, HashSet<ITypeSymbol> allTypes, SourceProductionContext context)
        {
            // Bug 4 fix: primitives/enums/ignored types never get a Pop<T> body emitted, so they
            // must NOT appear in `allTypeNames` either. If they did, every downstream dict/list/array
            // converter that checks `allTypeNames.Contains(itemTypeName)` would decide to emit a
            // call to the non-existent Pop<primitive> method. One bad registration (e.g.
            // `ApiResponse<int?>` at root) used to cascade into compile errors across unrelated
            // converters. Filter at the source.
            var allTypeNames = new HashSet<string>(
                allTypes.Where(t => t != null && !IsBlindSerializableType(t))
                        .Select(t => t!.ToDisplayString().Replace("?", "")));
            var typeName = TypeNameForPop(targetType);
            var converterName = $"{NameType(targetType)}JsonConverter";

            // Logging statement for allTypeNames
            Show($"{targetType.ToDisplayString()}: All registered type names: {string.Join(", ", allTypeNames)}", context);

            // Accumulates `private static readonly` declarations for [SubPropertyDefault(...)]
            // includes — one per attributed member in this target type. Emitted inline in the
            // partial-class fragment below so they co-locate with the Pop{T} method.
            var subPropertyDefaultFields = new StringBuilder();

            string internalSerializationCode = "";
            // Only complex-object targets get the split into a flag-computing 4-arg wrapper + an
            // Inner overload that takes pre-computed useAll/useDefault/naming. Collection, blind,
            // and nullable-wrapper targets have no flag-dependent body and keep the single 4-arg.
            bool emitInnerOverload = false;

            // Cycle-safety analysis: if the converter's effective payload type can never reach
            // itself (directly or through any property graph Popcorn recurses into), the visit-
            // tracking HashSet is dead weight. Skip the allocation at the entry point and let the
            // body's null-conditional ops no-op.
            var isCycleSafe = IsConverterCycleSafe(targetType, allTypeNames);
            Show($"{targetType.ToDisplayString()}: cycle-safe = {isCycleSafe}", context);

            // Bug 4 fix: root-level primitive / ignored / enum registration
            // (e.g. [JsonSerializable(typeof(ApiResponse<int?>))]). Emit a converter that simply
            // delegates to System.Text.Json — there is no Pop<int> method to dispatch to, and we
            // don't want to force the user to special-case these at the consumer level.
            if (IsBlindSerializableType(targetType))
            {
                Show($"{targetType} Is a blind-serializable (primitive/enum/ignored) type — emitting default JsonSerializer path", context);
                internalSerializationCode = @"
                    JsonSerializer.Serialize(writer, value.Data, options);
                ";
            }
            // Check if this is a nullable value type (Nullable<T>) whose underlying type is one
            // we DO emit a Pop<T> for. Primitives/enums wrapped in Nullable<T> are handled by the
            // IsBlindSerializableType branch above.
            else if (targetType is INamedTypeSymbol namedType &&
                namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                namedType.TypeArguments.Length == 1)
            {
                var underlyingType = namedType.TypeArguments[0];
                Show($"{targetType} Is a Nullable of {underlyingType.ToDisplayString()}", context);

                // Generate code to unwrap the nullable value and use the converter for the underlying type
                internalSerializationCode = $@"
                    // Unwrap the nullable value and use the converter for the underlying type
                    var unwrappedValue = value.Data.Value;
                    Pop{NameType(underlyingType)}(
                        writer,
                        new global::Popcorn.Shared.Pop<{TypeNameForPop(underlyingType)}> {{
                            Data = unwrappedValue,
                            PropertyReferences = value.PropertyReferences
                        }},
                        options, visitedObjects);
                ";
            }
            else if (targetType is INamedTypeSymbol namedDictionaryTypeNonNullable && InheritsOrImplements(namedDictionaryTypeNonNullable, IDictionaryTypeName))
            {
                // Covers Dictionary<K,V>, IDictionary<K,V>, ReadOnlyDictionary<K,V>, and any user
                // subclass that implements IDictionary<TKey, TValue>. A previous secondary check
                // keyed on Dictionary<K,V>'s OriginalDefinition existed to work around a whitespace
                // bug in IDictionaryTypeName that made this branch dead; the constant is now
                // correct ("IDictionary<TKey, TValue>" with the same ", " Roslyn emits), so the
                // secondary check is redundant and has been removed.
                var valueType = namedDictionaryTypeNonNullable.TypeArguments[1] as INamedTypeSymbol;
                Show($"DICTIONARY DETECTED: {namedDictionaryTypeNonNullable} Is an IDictionary of {valueType?.ToDisplayString()}", context);
                internalSerializationCode = CreateDictionarySerializer(allTypeNames, valueType, context);
            }
            // If this targetType implement IEnumerable, write out as an array and use the item type as target type instead for each element.
            else if (targetType is IArrayTypeSymbol arrayType)
            {
                var itemType = arrayType.ElementType as INamedTypeSymbol;
                Show($"{targetType} Is an Array of {itemType?.ToDisplayString()}", context);
                internalSerializationCode = CreateArraySerializer(allTypeNames, itemType);
            }
            else if (targetType is INamedTypeSymbol namedTypeNonNullable)
            {
                if (InheritsOrImplements(namedTypeNonNullable, IEnumerableTypeName))
                {
                    var itemType = namedTypeNonNullable.TypeArguments[0] as INamedTypeSymbol;
                    Show($"{namedTypeNonNullable} Is an IEnumerable of {itemType?.ToDisplayString()}", context);
                    internalSerializationCode = CreateArraySerializer(allTypeNames, itemType);
                }
                else
                {
                    internalSerializationCode = CreateComplexObjectInnerBody(namedTypeNonNullable, context, allTypeNames, subPropertyDefaultFields);
                    emitInnerOverload = true;
                }
            }
            else
            {
                // We shouldn't really get here, but if we do, just serialize the object normally
                internalSerializationCode = @"
                    // 411: Just serialize the field normally
                    JsonSerializer.Serialize(writer, value.Data, options);
                ";
            }

            var jsonContextName = classSymbol.ContainingNamespace.IsGlobalNamespace ? classSymbol.Name : $"{ classSymbol.ContainingNamespace}.{classSymbol.Name}";
            
            // First, determine if the type is nullable
            bool isNullable = IsNullableType(targetType);
            var nullCheck = "";
            if (isNullable)
            {
                nullCheck = $@"
                if(value.Data == null)
                {{
                    writer.WriteNullValue();
                    return;
                }}";
            }

            // The cast at every property-Pop callsite (`Data = (T)value.Data.Prop`) can still fire
            // CS8619 when Prop's declared type has nullable-element annotations (e.g. List<string?>)
            // that T (normalized by TypeNameForPop) lacks. Those two types are CLR-identical, so
            // the warning is noise in generated code. Suppress the nullability family at the file
            // level — we retain `#nullable enable` so member references and generic arguments keep
            // their precise typing, but CS8619 / CS8600 / CS8625 on the cast itself are silenced.
            return $@"// <auto-generated/>
        using System;
        using System.Text.Json;
        using System.Text.Json.Serialization;

        #nullable enable
        #pragma warning disable CS8619, CS8600, CS8601, CS8625
        namespace Popcorn.Generated.Converters
        {{
        public class {converterName} : global::System.Text.Json.Serialization.JsonConverter<global::Popcorn.Shared.Pop<{typeName}>>
        {{

        public override global::Popcorn.Shared.Pop<{typeName}> Read(ref Utf8JsonReader reader, Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
        {{
            throw new NotImplementedException();
        }}

        public override void Write(Utf8JsonWriter writer, global::Popcorn.Shared.Pop<{typeName}> value, global::System.Text.Json.JsonSerializerOptions options)
        {{
            {jsonContextName}.Pop{NameType(targetType)}(writer, value, options);
        }}
        }}
        }}

        {(classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {classSymbol.ContainingNamespace} {{")}
        {classSymbol.DeclaredAccessibility.ToString().ToLower()} partial class {classSymbol.Name}
        {{
        {subPropertyDefaultFields}
        public static void Pop{NameType(targetType)}(Utf8JsonWriter writer, global::Popcorn.Shared.Pop<{typeName}> value, global::System.Text.Json.JsonSerializerOptions options)
        {{
            Pop{NameType(targetType)}(writer, value, options, {(isCycleSafe ? "null" : "new HashSet<object>()")});
        }}

        {(emitInnerOverload ? $@"
        public static void Pop{NameType(targetType)}(Utf8JsonWriter writer, global::Popcorn.Shared.Pop<{typeName}> value, global::System.Text.Json.JsonSerializerOptions options, HashSet<object>? visitedObjects)
        {{
                {nullCheck}
                {FlagSetupCode}
                Pop{NameType(targetType)}Inner(writer, value, options, visitedObjects, naming, useAll, useDefault);
        }}

        public static void Pop{NameType(targetType)}Inner(Utf8JsonWriter writer, global::Popcorn.Shared.Pop<{typeName}> value, global::System.Text.Json.JsonSerializerOptions options, HashSet<object>? visitedObjects, Func<string, string> naming, bool useAll, bool useDefault)
        {{
                {nullCheck}
                {internalSerializationCode}
        }}
        " : $@"
        public static void Pop{NameType(targetType)}(Utf8JsonWriter writer, global::Popcorn.Shared.Pop<{typeName}> value, global::System.Text.Json.JsonSerializerOptions options, HashSet<object>? visitedObjects)
        {{
                {nullCheck}
                {internalSerializationCode}
        }}
        ")}
        }}
        {(classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : "}")}
        ";
        }

        private static string CreateArraySerializer(HashSet<string> allTypeNames, INamedTypeSymbol? itemType)
        {
            string internalSerializationCode;
            var propertyTypeName = itemType?.ToDisplayString().Replace("?", "");
            if (allTypeNames.Contains(propertyTypeName))
            {
                // Hoist useAll/useDefault/naming ONCE before the foreach — they depend only on
                // value.PropertyReferences + options, which are constant across items. Call the
                // element type's *Inner overload per item so it reuses these pre-computed values
                // instead of re-scanning + re-allocating a naming delegate per item.
                internalSerializationCode = $@"
                        {FlagSetupCode}
                        writer.WriteStartArray();
                        foreach(var item in value.Data)
                        {{
                            Pop{NameType(itemType)}Inner(
                                writer,
                                new global::Popcorn.Shared.Pop<{TypeNameForPop(itemType!)}> {{
                                    Data = item,
                                    PropertyReferences = value.PropertyReferences
                                }}, options, visitedObjects, naming, useAll, useDefault);
                        }}
                        writer.WriteEndArray();
    ";
            }
            else
            {
                // We just do a normal json serialize of the array // TODO << >> WHY DOES DICTIONARY SERIALIZATION GET HERE????
                internalSerializationCode = @"
                            // 493: Just serialize the field normally
                            JsonSerializer.Serialize(writer, value.Data, options);
    ";
            }

            return internalSerializationCode;
        }


        private static string CreateDictionarySerializer(HashSet<string> allTypeNames, INamedTypeSymbol? valueType, SourceProductionContext context)
        {
            string internalSerializationCode;
            var propertyTypeName = valueType?.ToDisplayString().Replace("?", "") ?? "";
            
            if (allTypeNames.Contains(propertyTypeName))
            {
                // We need to recurse into this type, and treat it as an array of bundles
                // This is... a wee bit ugly since each item needs to get bundled independently
                internalSerializationCode = $@"
                        // DICTIONARY COMPLEX PATH - Recursive dictionary serialization for {propertyTypeName}
                        // Hoist useAll/useDefault/naming ONCE before the foreach (constant across kv pairs)
                        // and call the value type's *Inner overload per value to skip per-item rescan.
                        {FlagSetupCode}
                        writer.WriteStartObject();

                        // value.PropertyReferences is the sibling list the outer converter drilled into
                        // for this dictionary property — i.e. the include list for each dictionary value.
                        // Pass it through verbatim; do NOT descend into firstRef.Children (that would lose
                        // siblings and confuse the parser's Default placeholder for a real child list).
                        var dictionaryValueReferences = value.PropertyReferences.Count > 0
                            ? value.PropertyReferences
                            : global::Popcorn.Shared.PropertyReference.Default;

                        foreach(var kv in value.Data)
                        {{
                                writer.WritePropertyName(naming(kv.Key));
                                Pop{NameType(valueType)}Inner(
                                    writer,
                                    new global::Popcorn.Shared.Pop<{TypeNameForPop(valueType!)}> {{
                                        Data = kv.Value,
                                        PropertyReferences = dictionaryValueReferences
                                }}, options, visitedObjects, naming, useAll, useDefault);
                        }}
                        writer.WriteEndObject();
";
            }
            else
            {
                // We just do a normal json serialize of the dictionary
                internalSerializationCode = @"
                            // DICTIONARY SIMPLE PATH - Just serialize the field normally
                        JsonSerializer.Serialize(writer, value.Data, options);
";
            }

            return internalSerializationCode;
        }

        // Yields public instance properties declared on `type` OR inherited from any base class
        // (excluding System.Object). When a derived type redeclares a name via `new` or `override`,
        // the derived declaration wins — we walk derived → base and dedupe on name.
        private static IEnumerable<IPropertySymbol> GetSerializableProperties(INamedTypeSymbol type)
        {
            var seen = new HashSet<string>();
            for (var current = type; current != null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
            {
                foreach (var member in current.GetMembers().OfType<IPropertySymbol>())
                {
                    if (member.DeclaredAccessibility != Accessibility.Public) continue;
                    if (member.GetMethod == null) continue;
                    if (member.IsIndexer) continue;
                    if (member.IsStatic) continue;
                    if (!seen.Add(member.Name)) continue;
                    yield return member;
                }
            }
        }

        private static IEnumerable<IFieldSymbol> GetSerializableFields(INamedTypeSymbol type)
        {
            var seen = new HashSet<string>();
            for (var current = type; current != null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
            {
                foreach (var member in current.GetMembers().OfType<IFieldSymbol>())
                {
                    if (member.DeclaredAccessibility != Accessibility.Public) continue;
                    if (member.IsStatic) continue;
                    if (member.IsConst) continue;
                    if (!seen.Add(member.Name)) continue;
                    yield return member;
                }
            }
        }

        // Helper methods for member serialization
        private static bool ShouldSerializeMember(ISymbol member)
        {
            var attrs = member.GetAttributes();
            return !attrs.Any(a => a.AttributeClass?.ToDisplayString() == "Popcorn.NeverAttribute");
        }

        private static string GetSerializedName(ISymbol member)
        {
            var attrs = member.GetAttributes();
            var nameAttr = attrs.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.Text.Json.Serialization.JsonPropertyNameAttribute");
            if (nameAttr != null && nameAttr.ConstructorArguments.Any())
            {
                return nameAttr.ConstructorArguments[0].Value?.ToString() ?? member.Name;
            }
            return member.Name;
        }

        private static bool HasAttribute(ISymbol member, string attributeTypeName)
        {
            return member.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attributeTypeName);
        }

        /// <summary>
        /// Returns the literal include-string argument of [SubPropertyDefault(...)] on the member,
        /// or null if the attribute isn't present or the argument isn't a string.
        /// </summary>
        private static string? GetSubPropertyDefaultIncludes(ISymbol member)
        {
            var attr = member.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString() == "Popcorn.SubPropertyDefaultAttribute");
            if (attr?.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is string s)
            {
                return s;
            }
            return null;
        }

        // Body of the *Inner overload: circular guard + property writes, assuming the caller has
        // already precomputed useAll / useDefault / naming. Callers that iterate a collection of
        // this type can compute the flags once and invoke the Inner method per item without
        // redoing the PropertyReferences scan or re-allocating the naming delegate.
        private static string CreateComplexObjectInnerBody(INamedTypeSymbol targetType, SourceProductionContext context, HashSet<string> allTypeNames, StringBuilder subPropertyDefaultFields)
        {
            var propertySerializationCode = new StringBuilder();
            var parentDiscriminator = NameType(targetType);

            // Walk declared + inherited members so [Always]/[Default] on a base class
            // are honored when serializing a derived type.
            var properties = GetSerializableProperties(targetType).ToList();
            var fields = GetSerializableFields(targetType).ToList();

            bool hasAlwaysOrDefaultAttribute =
                properties.Any(p => HasAttribute(p, "Popcorn.AlwaysAttribute") || HasAttribute(p, "Popcorn.DefaultAttribute")) ||
                fields.Any(f => HasAttribute(f, "Popcorn.AlwaysAttribute") || HasAttribute(f, "Popcorn.DefaultAttribute"));

            foreach (var property in properties)
            {
                if (!ShouldSerializeMember(property))
                {
                    continue;
                }

                AddMemberSerializationCode(property, property.Type, property.Name, false, propertySerializationCode, context, allTypeNames, hasAlwaysOrDefaultAttribute, parentDiscriminator, subPropertyDefaultFields);
            }

            foreach (var field in fields)
            {
                if (!ShouldSerializeMember(field))
                {
                    continue;
                }

                AddMemberSerializationCode(field, field.Type, field.Name, true, propertySerializationCode, context, allTypeNames, hasAlwaysOrDefaultAttribute, parentDiscriminator, subPropertyDefaultFields);
            }

            var innerBody = $@"
                // Circular reference detection. Cycle-safe converter entry points pass null here
                // (see IsConverterCycleSafe), so the ops are null-conditional.
                if (visitedObjects != null)
                {{
                    if (visitedObjects.Contains(value.Data))
                    {{
                        writer.WriteStartObject();
                        writer.WritePropertyName(""$ref"");
                        writer.WriteStringValue(""circular"");
                        writer.WriteEndObject();
                        return;
                    }}
                    visitedObjects.Add(value.Data);
                }}

                var properties = value.PropertyReferences;

                writer.WriteStartObject();

                {propertySerializationCode}

                writer.WriteEndObject();

                if (visitedObjects != null) visitedObjects.Remove(value.Data);
            ";
            return innerBody;
        }

        // Code that precomputes useAll / useDefault / naming from value.PropertyReferences + options.
        // Shared between the 4-arg Pop{X} wrapper and any caller that wants to hoist the computation
        // (e.g. a list/dict converter iterating items with a shared PropertyReferences list).
        private const string FlagSetupCode = @"
                Func<string, string> naming = options.PropertyNamingPolicy != null ? options.PropertyNamingPolicy.ConvertName : (a) => a;
                var __flagScanRefs = value.PropertyReferences;
                bool useAll = false;
                bool useDefault = __flagScanRefs.Count == 0;
                for (int __markerIdx = 0; __markerIdx < __flagScanRefs.Count; __markerIdx++)
                {
                    var __markerName = __flagScanRefs[__markerIdx].Name.Span;
                    if (!useAll && ""!all"".AsSpan().Equals(__markerName, StringComparison.Ordinal)) useAll = true;
                    if (!useDefault && ""!default"".AsSpan().Equals(__markerName, StringComparison.Ordinal)) useDefault = true;
                }";

        private static void AddMemberSerializationCode(
            ISymbol member,
            ITypeSymbol memberType,
            string memberName,
            bool isField,
            StringBuilder codeBuilder,
            SourceProductionContext context,
            HashSet<string> allTypeNames,
            bool hasAlwaysOrDefaultAttribute,
            string? parentDiscriminator,
            StringBuilder subPropertyDefaultFields)
        {
            var serializedName = GetSerializedName(member);
            var originalName = memberName;
            var referenceName = $"value.Data.{originalName}";
            var serializeLine = @$"
                // 646: Just serialize the field normally
                JsonSerializer.Serialize(writer, {referenceName}, options);";

            var memberTypeName = memberType.ToDisplayString().Replace("?", "");
            var handleNullable = IsNullableType(memberType);
            Show($"Will Render {memberTypeName} {(isField ? "field" : "property")}", context);

            // SubPropertyDefault: when the member carries [SubPropertyDefault("[X,Y]")] we emit a
            // process-level static readonly field holding the pre-parsed include list, and we
            // substitute the "no explicit children" fallback so it resolves to that list instead
            // of the PropertyReference.Default singleton. Still allocates nothing per-request.
            var subPropertyDefaultIncludes = GetSubPropertyDefaultIncludes(member);
            string subPropertyDefaultFieldName = null!;
            string childReferencesExpression =
                "propertyReference?.Children ?? global::Popcorn.Shared.PropertyReference.Default";
            if (subPropertyDefaultIncludes != null && parentDiscriminator != null)
            {
                subPropertyDefaultFieldName = $"__SubDefault_{parentDiscriminator}_{originalName}";
                var escaped = subPropertyDefaultIncludes.Replace("\\", "\\\\").Replace("\"", "\\\"");
                subPropertyDefaultFields.AppendLine($@"
        private static readonly global::System.Collections.Generic.IReadOnlyList<global::Popcorn.Shared.PropertyReference> {subPropertyDefaultFieldName} =
            global::Popcorn.Shared.PropertyReference.ParseIncludeStatement(""{escaped}"");");
                childReferencesExpression =
                    $"(propertyReference == null || object.ReferenceEquals(propertyReference.Children, global::Popcorn.Shared.PropertyReference.Default)) ? {subPropertyDefaultFieldName} : propertyReference.Children";
            }
            
            // Serialization logic for different types
            if (IgnoreTypes.Contains(memberTypeName))
            {
                // No change; just defer to the serializer.
            }
            else if (NumberTypes.Contains(memberTypeName))
            {
                if (handleNullable)
                {
                    serializeLine = $"if({referenceName} == null) {{ writer.WriteNullValue(); }} else {{ writer.WriteNumberValue({referenceName}{(handleNullable ? ".Value" : "")}); }}";
                }
                else
                {
                    serializeLine = $"writer.WriteNumberValue({referenceName}{(handleNullable ? ".Value" : "")});";
                }
            }
            else if (StringTypes.Contains(memberTypeName))
            {
                serializeLine = $"writer.WriteStringValue({referenceName});";
            }
            else if (BoolTypes.Contains(memberTypeName))
            {
                if (handleNullable)
                {
                    serializeLine = $"if({referenceName} == null) {{ writer.WriteNullValue(); }} else {{ writer.WriteBooleanValue({referenceName}{(handleNullable ? ".Value" : "")}); }}";
                }
                else
                {
                    serializeLine = $"writer.WriteBooleanValue({referenceName}{(handleNullable ? ".Value" : "")});";
                }
            }
            else if (memberType is IArrayTypeSymbol arrayType)
            {
                // Handle array type
                var elementTypeName = arrayType.ElementType.ToDisplayString().Replace("?", "");
                if (allTypeNames.Contains(elementTypeName) && arrayType.ElementType is INamedTypeSymbol elementNamedType)
                {
                    // We need to recurse into each element of the array
                    serializeLine = $@"
                    if({referenceName} == null) {{
                        writer.WriteNullValue();
                    }}
                    else
                    {{
                        // 695: We need to recurse into this type, and treat it as an array of bundles
                        writer.WriteStartArray();
                        foreach(var item in {referenceName})
                        {{
                            Pop{NameType(elementNamedType)}(
                                writer,
                                new global::Popcorn.Shared.Pop<{TypeNameForPop(arrayType.ElementType)}> {{
                                    Data = item,
                                    PropertyReferences = {childReferencesExpression}
                                }}, options, visitedObjects);
                        }}
                        writer.WriteEndArray();
                    }}";
                }
                else
                {
                    // Just serialize the array normally
                    serializeLine = @$"
                        // 713: Just serialize the array normally
                        JsonSerializer.Serialize(writer, {referenceName}, options);";
                }
            }
            else if (allTypeNames.Contains(memberTypeName) && memberType is INamedTypeSymbol memberNamedType)
            {
                // if this were supported for Popping, then wrap in another Pop
                var nullCheck = "";
                if (handleNullable)
                {
                    nullCheck = $@"if({referenceName} == null) 
                        {{ writer.WriteNullValue(); }} 
                        else 
                        ";
                }
                serializeLine = $@"{nullCheck}
                {{
                    // 730: We need to recurse into this type, and treat it as a bundle
                    Pop{NameType(memberNamedType)}(
                        writer,
                        new global::Popcorn.Shared.Pop<{TypeNameForPop(memberType)}>
                        {{
                            Data = ({TypeNameForPop(memberType)}){referenceName},
                            PropertyReferences = {childReferencesExpression}
                        }},
                        options, visitedObjects);
                }}";
            }
            
            var serializeGroup = "";
            
            // Is this _Always_ included? Then include it.
            if (HasAttribute(member, "Popcorn.AlwaysAttribute"))
            {
                serializeGroup = ($@"
            // Always properties must ALWAYS be included, even when explicitly negated
            // This is the core contract of the [Always] attribute
            {{
                // {memberTypeName} {originalName} ({(isField ? "field" : "property")})
                writer.WritePropertyName(naming(""{serializedName}""));
                {serializeLine}
            }}");
            }
            // Is this included by !default Then include it unless excluded
            else if (HasAttribute(member, "Popcorn.DefaultAttribute") || (!hasAlwaysOrDefaultAttribute && !HasAttribute(member, "Popcorn.NeverAttribute")))
            {
                serializeGroup = ($@"
            if((useAll || useDefault || propertyReference != null) && (propertyReference == null || propertyReference.Negated == false))
            {{
                // {memberTypeName} {originalName} ({(isField ? "field" : "property")})
                writer.WritePropertyName(naming(""{serializedName}""));
                {serializeLine}
            }}");
            }
            else
            {
                serializeGroup = ($@"
            if((useAll || propertyReference != null) && (propertyReference == null || propertyReference.Negated == false))
            {{
                // {memberTypeName} {originalName} ({(isField ? "field" : "property")})
                writer.WritePropertyName(naming(""{serializedName}""));
                {serializeLine}
            }}");
            }
            
            codeBuilder.AppendLine($@"
        {{
            // Find if this specific member is requested (for-loop instead of LINQ to avoid per-property enumerator allocation)
            global::Popcorn.Shared.PropertyReference? propertyReference = null;
            for (int __refIdx = 0; __refIdx < properties.Count; __refIdx++)
            {{
                var __refCandidate = properties[__refIdx];
                if (""{serializedName}"".AsSpan().Equals(__refCandidate.Name.Span, StringComparison.Ordinal))
                {{
                    propertyReference = __refCandidate;
                    break;
                }}
            }}
            {serializeGroup}
        }}");
        }

        private static string? NameType(ITypeSymbol? type)
        {
            if (type == null)
            {
                return null;
            }

            if (type is IArrayTypeSymbol arrayType)
            {
                return $"ArrayOf{NameType(arrayType.ElementType)}";
            }
            
            if (type is INamedTypeSymbol namedType)
            {
                var ns = namedType.ContainingNamespace.IsGlobalNamespace ? null : namedType.ContainingNamespace.ToString().Replace(".", "");
                
                if (!namedType.IsGenericType)
                {
                    return $"{ns}{namedType.Name}";
                }

                var paramTypes = String.Join("", namedType.TypeArguments.Select(t => NameType(t)));
                return $"{ns}{namedType.Name}{paramTypes}";
            }
            
            // Fallback for other types
            return type.Name;
        }


        public static void Show(string message, SourceProductionContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "JSG002",
                    title: "Output",
                    messageFormat: message,
                    category: "SourceGenerator",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                Location.None));
        }
    }

    internal record GeneratorClassReference(INamedTypeSymbol? ClassSymbol, IEnumerable<AttributeData> Attributes);
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
