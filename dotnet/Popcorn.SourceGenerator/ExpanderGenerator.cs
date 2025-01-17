using Microsoft.CodeAnalysis;
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
        private static readonly HashSet<string> NumberTypes = new HashSet<string>([
            typeof(Single).FullName,
            typeof(Double).FullName,
            typeof(Decimal).FullName,
            typeof(Byte).FullName,
            typeof(UInt16).FullName,
            typeof(UInt32).FullName,
            typeof(UInt64).FullName,
            typeof(SByte).FullName,
            typeof(Int16).FullName,
            typeof(Int32).FullName,
            typeof(Int64).FullName,
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
            typeof(string).FullName,
            typeof(Span<char>).FullName,
            typeof(ReadOnlySpan<char>).FullName,
            typeof(Memory<char>).FullName,
            typeof(Guid).FullName,
            typeof(DateTime).FullName,
            typeof(DateTimeOffset).FullName,
            typeof(ReadOnlyMemory<char>).FullName
        ]);

        private static readonly HashSet<string> BoolTypes = new HashSet<string>([
            typeof(bool).FullName,
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

            // Step 3: Generate the JsonConverter class for each target type
            context.RegisterSourceOutput(jsonSerializableAttributes, (spc, data) =>
            {
                var targetTypes = new HashSet<INamedTypeSymbol>(
                    data.Attributes
                        .Select(attribute => attribute.ConstructorArguments[0].Value as INamedTypeSymbol)
                        .Where(a => a != null && a.TypeArguments.Length > 0)
                        .Select(a => a.TypeArguments[0] as INamedTypeSymbol)
                        .Where(a => a != null)!,
                    SymbolEqualityComparer.Default);
                foreach (var targetType in targetTypes)
                {
                    try
                    {
                        var source = GenerateJsonConverter(targetType, data.ClassSymbol, targetTypes);
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

public static partial class PopcornJsonOptionsExtension
{{
    public static void AddPopcornOptions(this global::System.Text.Json.JsonSerializerOptions options)
    {{
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
            while (typeSymbol != null)
            {
                if (typeSymbol.OriginalDefinition.ToDisplayString() == baseTypeName)
                {
                    return true;
                }
                typeSymbol = typeSymbol.BaseType;
            }
            return false;
        }

        private static string GenerateJsonConverter(INamedTypeSymbol targetType, INamedTypeSymbol? classSymbol, HashSet<INamedTypeSymbol> allTypes)
        {
            // We need to build out the recursive references here
            // Visit each type and find each property that could be serialized and ensure that its type is added to the list.
            var visitedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var typesToVisit = new Queue<INamedTypeSymbol>(allTypes);

            while (typesToVisit.Count > 0)
            {
                var currentType = typesToVisit.Dequeue();
                if (!visitedTypes.Add(currentType))
                {
                    continue;
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

            
            var allTypeNames = new HashSet<string>(allTypes.Union(allTypes, SymbolEqualityComparer.Default).Where(t => t!=null).Select(t => t!.ToDisplayString()));


            var typeName = targetType.ToDisplayString();
            var converterName = $"{NameType(targetType)}JsonConverter";
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

                var propertyType = property.Type.ToDisplayString();

                var serializeLine = $"JsonSerializer.Serialize(writer, value.Data.{originalName}, options);";
                if (allTypeNames.Contains(propertyType))
                {
                    // if this were supported for Popping, then wrap in another Pop
                    serializeLine = $@"if(value == null) 
                    {{ writer.WriteNullValue(); }} 
                    else 
                    {{ 
                        Pop{NameType(property.Type as INamedTypeSymbol)}(
                            writer, 
                            new global::Popcorn.Shared.Pop<{propertyType}> 
                            {{ 
                                Data = value.Data.{originalName}, 
                                PropertyReferences = propertyReference?.Children ?? global::System.Collections.Immutable.ImmutableArray<global::Popcorn.Shared.PropertyReference>.Empty
                            }}, 
                            options); 
                    }}";
                }
                else if (NumberTypes.Contains(propertyType))
                {
                    serializeLine = $"writer.WriteNumberValue(value.Data.{originalName});";
                }
                else if (StringTypes.Contains(propertyType))
                {
                    serializeLine = $"writer.WriteStringValue(value.Data.{originalName});";
                }
                else if (BoolTypes.Contains(propertyType))
                {
                    serializeLine = $"writer.WriteBooleanValue(value.Data.{originalName});";
                }

                propertySerializationCode.AppendLine($@"
            {{
                // Find if this specific property is requested
                var propertyReference = properties.FirstOrDefault(p => ""{propertyName}"".AsSpan().Equals(p.Name.Span, StringComparison.Ordinal));");

                // Is this _Always_ included? Then include it.
                if (subAttrs.Any(a => a.AttributeClass?.ToString() == typeof(Popcorn.AlwaysAttribute).FullName))
                {
                    propertySerializationCode.AppendLine($@"
                if(propertyReference == null || propertyReference.Negated == false)
                {{
                    // {propertyType} {originalName}
                    writer.WritePropertyName(""{propertyName}"");
                    {serializeLine}
                }}");
                }
                // Is this included by !default Then include it unless excluded
                else if (subAttrs.Any(a => a.AttributeClass?.ToString() == typeof(Popcorn.DefaultAttribute).FullName))
                {
                    propertySerializationCode.AppendLine($@"
                if((useAll || useDefault || propertyReference != null) && (propertyReference == null || propertyReference.Negated == false))
                {{
                    // {propertyType} {originalName}
                    writer.WritePropertyName(""{propertyName}"");
                    {serializeLine}
                }}");
                }
                else
                {
                    propertySerializationCode.AppendLine($@"
                if((useAll || propertyReference != null) && (propertyReference == null || propertyReference.Negated == false))
                {{
                    // {propertyType} {originalName}
                    writer.WritePropertyName(""{propertyName}"");
                    {serializeLine}
                }}");
                }

                propertySerializationCode.AppendLine($@"
            }}");
            }

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
            AppJsonSerializerContext.Pop{NameType(targetType)}(writer, value, options);
        }}
    }}
}}

{(classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {classSymbol.ContainingNamespace} {{")}
    [System.Text.Json.Serialization.JsonSerializable(typeof(Popcorn.Shared.Pop<{typeName}>))]
    {classSymbol.DeclaredAccessibility.ToString().ToLower()} partial class {classSymbol.Name}
    {{
        public static void Pop{NameType(targetType)}(Utf8JsonWriter writer, global::Popcorn.Shared.Pop<{typeName}> value, global::System.Text.Json.JsonSerializerOptions options)
        {{
                var properties = value.PropertyReferences;
                var useAll = properties.Any(p => ""!all"".AsSpan().Equals(p.Name.Span, StringComparison.Ordinal));
                // Use default includes if either nothing is explicitly called out or if defaults are explicitly requested
                var useDefault = !properties.Any() || properties.Any(p => ""!default"".AsSpan().Equals(p.Name.Span, StringComparison.Ordinal));

                writer.WriteStartObject();
            
                {propertySerializationCode}

                writer.WriteEndObject();
        }}
    }}
{(classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : "}}")}
";
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


        private static string? GetNamedArgument(AttributeData attributeData, string argName)
        {
            // This is the attribute, check all of the named arguments
            foreach (KeyValuePair<string, TypedConstant> namedArgument in attributeData.NamedArguments)
            {
                // Is this the ExtensionClassName argument?
                if (namedArgument.Key == argName
                    && namedArgument.Value.Value?.ToString() is { } n)
                {
                    return n;
                }
            }

            return null;
        }
    }

    internal record GeneratorClassReference(INamedTypeSymbol? ClassSymbol, IEnumerable<AttributeData> Attributes);
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}