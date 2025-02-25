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
        private const string IDictionaryTypeName = "System.Collections.Generic.IDictionary<TKey,TValue>";
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

                var targetTypes = new HashSet<INamedTypeSymbol>(
                    data.Attributes
                        .Select(attribute => attribute.ConstructorArguments[0].Value as INamedTypeSymbol)
                        .Where(a => a != null && a.TypeArguments.Length > 0)
                        .Select(a => a?.TypeArguments[0] as INamedTypeSymbol)
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
                    // Now add the top-level extension method for registering all our converters to the WebApi pipeline
                    spc.AddSource("RegisterConverters.g.cs", SourceText.From($@"
namespace Popcorn.Shared;

public static class PopcornJsonOptionsExtension
{{
    public static void AddPopcornOptions(this global::System.Text.Json.JsonSerializerOptions options)
    {{
        options.NumberHandling = global::System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
        {String.Join("", targetTypes.Select(targetType => $@"
        options.Converters.Add(new global::Popcorn.Generated.Converters.{NameType(targetType)}JsonConverter());")
            )}
    }}
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
                    && InheritsOrImplements(typeSymbol, "Popcorn.Shared.ApiResponse<T>"));
        }

        private static bool InheritsOrImplements(INamedTypeSymbol typeSymbol, string baseTypeName)
        {
            var visitedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var typesToVisit = new Queue<INamedTypeSymbol>();
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

        private static HashSet<INamedTypeSymbol> GetReferencedTypes(INamedTypeSymbol targetType, INamedTypeSymbol classSymbol, SourceProductionContext context)
        {
            // We need to build out the recursive references here
            // Visit each type and find each property that could be serialized and ensure that its type is added to the list.
            var visitedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var typesToVisit = new Queue<INamedTypeSymbol>();
            typesToVisit.Enqueue(targetType);

            while (typesToVisit.Count > 0)
            {
                var currentType = typesToVisit.Dequeue();
                var propertyTypeName = currentType.ToDisplayString().Replace("?", "");
                if (IgnoreTypes.Contains(propertyTypeName) 
                    || NumberTypes.Contains(propertyTypeName)
                    || StringTypes.Contains(propertyTypeName)
                    || BoolTypes.Contains(propertyTypeName))
                {
                    continue;
                }

                if (!visitedTypes.Add(currentType))
                {
                    continue;
                }

                // Check if this is a collection type and extract its item type
                if (currentType.OriginalDefinition != null)
                {
                    if (InheritsOrImplements(targetType, IEnumerableTypeName) &&
                    currentType.TypeArguments.Length > 0)
                    {
                        var itemType = currentType.TypeArguments[0] as INamedTypeSymbol;
                        if (itemType != null && !visitedTypes.Contains(itemType) && !typesToVisit.Contains(itemType))
                        {
                            typesToVisit.Enqueue(itemType);
                        }
                    }
                    // Handle IDictionary<K,V> - only consider the value type V, not the key type K
                    else if (InheritsOrImplements(currentType, IDictionaryTypeName) &&
                             currentType.TypeArguments.Length > 1)
                    {
                        var valueType = currentType.TypeArguments[1] as INamedTypeSymbol;
                        if (valueType != null && !visitedTypes.Contains(valueType) && !typesToVisit.Contains(valueType))
                        {
                            typesToVisit.Enqueue(valueType);
                        }
                    }
                }

                foreach (var member in currentType.GetMembers().OfType<IPropertySymbol>())
                {
                    if (member.DeclaredAccessibility == Accessibility.Public && member.GetMethod != null && !member.IsIndexer)
                    {
                        var memberType = member.Type as INamedTypeSymbol;
                        if (memberType != null && !visitedTypes.Contains(memberType) && !typesToVisit.Contains(memberType))
                        {
                            typesToVisit.Enqueue(memberType);
                        }
                    }
                }
            }

            return visitedTypes;
        }

        private static string GenerateJsonConverter(INamedTypeSymbol targetType, INamedTypeSymbol classSymbol, HashSet<INamedTypeSymbol> allTypes, SourceProductionContext context)
        {
            var allTypeNames = new HashSet<string>(allTypes.Where(t => t != null).Select(t => t!.ToDisplayString().Replace("?", "")));
            var typeName = targetType.ToDisplayString();
            var converterName = $"{NameType(targetType)}JsonConverter";

            // Logging statement for allTypeNames
            Show($"{targetType.ToDisplayString()}: All registered type names: {string.Join(", ", allTypeNames)}", context);

            string internalSerializationCode = "";
            // If this targetType implement IEnumerable, write out as an array and use the item type as target type instead for each element.
            if (InheritsOrImplements(targetType, IEnumerableTypeName))
            {
                var itemType = targetType.TypeArguments[0] as INamedTypeSymbol;
                Show($"{targetType} Is an IEnumerable of {itemType?.ToDisplayString()}", context);
                var propertyTypeName = itemType.ToDisplayString().Replace("?", "");
                if (allTypeNames.Contains(propertyTypeName))
                {
                    // We need to recurse into this type, and treat it as an array of bundles
                    // This is... a wee bit ugly since each item needs to get bundled independently
                    internalSerializationCode = $@"
                        writer.WriteStartArray();
                        foreach(var item in value.Data)
                        {{
                            Pop{NameType(itemType)}(
                                writer, 
                                new global::Popcorn.Shared.Pop<{itemType.ToDisplayString()}> {{ 
                                    Data = item, 
                                    PropertyReferences = value.PropertyReferences 
                                }}, options);
                        }}  
                        writer.WriteEndArray();
";
                }
                else
                {
                    // We just do a normal json serialize of the array
                    internalSerializationCode = @"
                        JsonSerializer.Serialize(writer, value.Data, options);
";
                }
            }
            else
            {
                internalSerializationCode = CreateComplexObjectSerialization(targetType, context, allTypeNames);
            }

            var jsonContextName = classSymbol.ContainingNamespace.IsGlobalNamespace ? classSymbol.Name : $"{classSymbol.ContainingNamespace}.{classSymbol.Name}";
            
            return $@"// <auto-generated/>
        using System;
        using System.Text.Json;
        using System.Text.Json.Serialization;

        #nullable enable
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
        public static void Pop{NameType(targetType)}(Utf8JsonWriter writer, global::Popcorn.Shared.Pop<{typeName}> value, global::System.Text.Json.JsonSerializerOptions options)
        {{
                if(value.Data == null)
                {{
                    writer.WriteNullValue();
                    return;
                }}

                {internalSerializationCode}
        }}
        }}
        {(classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : "}")}
        ";
        }

        private static string CreateComplexObjectSerialization(INamedTypeSymbol targetType, SourceProductionContext context, HashSet<string> allTypeNames)
        {
            var properties = targetType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(prop => prop.DeclaredAccessibility == Accessibility.Public && prop.GetMethod != null && !prop.IsIndexer)
                .ToList();

            var propertySerializationCode = new StringBuilder();

            // Create the type converter

            foreach (var property in properties)
            {
                var subAttrs = property.GetAttributes();
                if (subAttrs.Any(a => a.AttributeClass?.ToString() == typeof(Popcorn.NeverAttribute).FullName))
                {
                    continue;
                }

                var propertyName = property.Name;
                var originalName = propertyName;

                var nameAttr = subAttrs.FirstOrDefault(a => a.AttributeClass?.ToString() == "System.Text.Json.Serialization.JsonPropertyNameAttribute");
                if (nameAttr != null && nameAttr.ConstructorArguments.Any())
                {
                    propertyName = nameAttr.ConstructorArguments[0].Value?.ToString() ?? originalName;
                }

                var propertyType = (INamedTypeSymbol)property.Type;
                var referenceName = $"value.Data.{originalName}";

                var serializeLine = $"JsonSerializer.Serialize(writer, {referenceName}, options);";

                var propertyTypeName = property.Type.ToDisplayString().Replace("?", "");
                var isNullable = property.Type.ToDisplayString().Contains("?");
                Show($"Will Render {propertyTypeName}", context);
                if (IgnoreTypes.Contains(propertyTypeName))
                {
                    // No  change; just defer to the serializer.
                }
                else if (NumberTypes.Contains(propertyTypeName))
                {
                    if (isNullable)
                    {
                        serializeLine = $"if({referenceName} == null) {{ writer.WriteNullValue(); }} else {{ writer.WriteNumberValue({referenceName}{(isNullable ? ".Value" : "")}); }}";
                    }
                    else
                    {
                        serializeLine = $"writer.WriteNumberValue({referenceName}{(isNullable ? ".Value" : "")});";
                    }
                }
                else if (StringTypes.Contains(propertyTypeName))
                {
                    serializeLine = $"writer.WriteStringValue({referenceName});";
                }
                else if (BoolTypes.Contains(propertyTypeName))
                {
                    if (isNullable)
                    {
                        serializeLine = $"if({referenceName} == null) {{ writer.WriteNullValue(); }} else {{ writer.WriteBooleanValue({referenceName}{(isNullable ? ".Value" : "")}); }}";
                    }
                    else
                    {
                        serializeLine = $"writer.WriteBooleanValue({referenceName}{(isNullable ? ".Value" : "")});";
                    }
                }
                else if (allTypeNames.Contains(propertyTypeName))
                {
                    // if this were supported for Popping, then wrap in another Pop
                    serializeLine = $@"if({referenceName} == null) 
                    {{ writer.WriteNullValue(); }} 
                    else 
                    {{ 
                        Pop{NameType(property.Type as INamedTypeSymbol)}(
                            writer, 
                            new global::Popcorn.Shared.Pop<{property.Type.ToDisplayString()}> 
                            {{ 
                                Data = ({property.Type.ToDisplayString()}){referenceName}, 
                                PropertyReferences = propertyReference?.Children ?? global::Popcorn.Shared.PropertyReference.Default
                            }}, 
                            options); 
                    }}";
                }

                var serializeGroup = "";

                // Is this _Always_ included? Then include it.
                if (subAttrs.Any(a => a.AttributeClass?.ToString() == typeof(Popcorn.AlwaysAttribute).FullName))
                {
                    serializeGroup = ($@"
                if(propertyReference == null || propertyReference.Negated == false)
                {{
                    // {propertyTypeName} {originalName}
                    writer.WritePropertyName(naming(""{propertyName}""));
                    {serializeLine}
                }}");
                }
                // Is this included by !default Then include it unless excluded
                else if (subAttrs.Any(a => a.AttributeClass?.ToString() == typeof(Popcorn.DefaultAttribute).FullName))
                {
                    serializeGroup = ($@"
                if((useAll || useDefault || propertyReference != null) && (propertyReference == null || propertyReference.Negated == false))
                {{
                    // {propertyTypeName} {originalName}
                    writer.WritePropertyName(naming(""{propertyName}""));
                    {serializeLine}
                }}");
                }
                else
                {
                    serializeGroup = ($@"
                if((useAll || propertyReference != null) && (propertyReference == null || propertyReference.Negated == false))
                {{
                    // {propertyTypeName} {originalName}
                    writer.WritePropertyName(naming(""{propertyName}""));
                    {serializeLine}
                }}");
                }

                propertySerializationCode.AppendLine($@"
            {{
                // Find if this specific property is requested
                var propertyReference = properties.FirstOrDefault(p => ""{propertyName}"".AsSpan().Equals(p.Name.Span, StringComparison.Ordinal));
                {serializeGroup}
            }}");
            }

            var internalSerializationCode = $@"
                Func<string, string> naming = options.PropertyNamingPolicy != null ? options.PropertyNamingPolicy.ConvertName : (a) => a;
                var properties = value.PropertyReferences;
                var useAll = properties.Any(p => ""!all"".AsSpan().Equals(p.Name.Span, StringComparison.Ordinal));
                // Use default includes if either nothing is explicitly called out or if defaults are explicitly requested
                var useDefault = !properties.Any() || properties.Any(p => ""!default"".AsSpan().Equals(p.Name.Span, StringComparison.Ordinal));

                writer.WriteStartObject();
            
                {propertySerializationCode}

                writer.WriteEndObject();
";
            return internalSerializationCode;
        }

        private static string? NameType(INamedTypeSymbol? type)
        {
            if (type == null)
            {
                return null;
            }

            var ns = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToString().Replace(".", "");
            if (!type.IsGenericType)
            {
                return $"{ns}{type.Name}";
            }

            var paramTypes = String.Join("", type.TypeArguments.Select(t => NameType(t as INamedTypeSymbol)));
            return $"{ns}{type.Name}{paramTypes}";
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
