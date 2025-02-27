﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using Microsoft.CodeAnalysis;
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

                    if (!visitedTypes.Add(namedType))
                    {
                        continue;
                    }

                    // Check if this is a collection type and extract its item type
                    if (namedType.OriginalDefinition != null)
                    {
                        if (InheritsOrImplements(namedType, IEnumerableTypeName) &&
                            namedType.TypeArguments.Length > 0)
                        {
                            var itemType = namedType.TypeArguments[0];
                            if (itemType is INamedTypeSymbol itemNamedType && !visitedTypes.Contains(itemNamedType))
                            {
                                typesToVisit.Enqueue(itemNamedType);
                            }
                        }
                        // Handle IDictionary<K,V> - only consider the value type V, not the key type K
                        else if (InheritsOrImplements(namedType, IDictionaryTypeName) &&
                                namedType.TypeArguments.Length > 1)
                        {
                            var valueType = namedType.TypeArguments[1];
                            if (valueType is INamedTypeSymbol valueNamedType && !visitedTypes.Contains(valueNamedType))
                            {
                                typesToVisit.Enqueue(valueNamedType);
                            }
                        }
                    }

                    // Process properties
                    foreach (var member in namedType.GetMembers().OfType<IPropertySymbol>())
                    {
                        if (member.DeclaredAccessibility == Accessibility.Public && member.GetMethod != null && !member.IsIndexer)
                        {
                            typesToVisit.Enqueue(member.Type);
                        }
                    }
                    
                    // Process fields - NEW CODE
                    foreach (var member in namedType.GetMembers().OfType<IFieldSymbol>())
                    {
                        if (member.DeclaredAccessibility == Accessibility.Public && !member.IsStatic && !member.IsConst)
                        {
                            typesToVisit.Enqueue(member.Type);
                        }
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
            var allTypeNames = new HashSet<string>(allTypes.Where(t => t != null).Select(t => t!.ToDisplayString().Replace("?", "")));
            var typeName = targetType.ToDisplayString();
            var converterName = $"{NameType(targetType)}JsonConverter";

            // Logging statement for allTypeNames
            Show($"{targetType.ToDisplayString()}: All registered type names: {string.Join(", ", allTypeNames)}", context);

            string internalSerializationCode = "";
            
            // Check if this is a nullable value type (Nullable<T>)
            if (targetType is INamedTypeSymbol namedType && 
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
                        new global::Popcorn.Shared.Pop<{underlyingType.ToDisplayString()}> {{ 
                            Data = unwrappedValue, 
                            PropertyReferences = value.PropertyReferences 
                        }}, 
                        options);
                ";
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
                    internalSerializationCode = CreateComplexObjectSerialization(namedTypeNonNullable, context, allTypeNames);
                }
            }
            else
            {
                // We shouldn't really get here, but if we do, just serialize the object normally
                internalSerializationCode = @"
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
                {nullCheck}
                {internalSerializationCode}
        }}
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

            return internalSerializationCode;
        }

        // Helper methods for member serialization
        private static bool ShouldSerializeMember(ISymbol member)
        {
            var attrs = member.GetAttributes();
            return !attrs.Any(a => a.AttributeClass?.ToString() == typeof(Popcorn.NeverAttribute).FullName);
        }

        private static string GetSerializedName(ISymbol member)
        {
            var attrs = member.GetAttributes();
            var nameAttr = attrs.FirstOrDefault(a => a.AttributeClass?.ToString() == "System.Text.Json.Serialization.JsonPropertyNameAttribute");
            if (nameAttr != null && nameAttr.ConstructorArguments.Any())
            {
                return nameAttr.ConstructorArguments[0].Value?.ToString() ?? member.Name;
            }
            return member.Name;
        }

        private static bool HasAttribute(ISymbol member, string attributeTypeName)
        {
            return member.GetAttributes().Any(a => a.AttributeClass?.ToString() == attributeTypeName);
        }

        private static string CreateComplexObjectSerialization(INamedTypeSymbol targetType, SourceProductionContext context, HashSet<string> allTypeNames)
        {
            var propertySerializationCode = new StringBuilder();
            
            // Check if any member has Always or Default attribute
            bool hasAlwaysOrDefaultAttribute = false;
            
            // Check properties for Always or Default attributes
            foreach (var property in targetType.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.GetMethod != null && !p.IsIndexer))
            {
                if (HasAttribute(property, typeof(Popcorn.AlwaysAttribute).FullName) || 
                    HasAttribute(property, typeof(Popcorn.DefaultAttribute).FullName))
                {
                    hasAlwaysOrDefaultAttribute = true;
                    break;
                }
            }
            
            // If no property has Always/Default, check fields
            if (!hasAlwaysOrDefaultAttribute)
            {
                foreach (var field in targetType.GetMembers().OfType<IFieldSymbol>()
                    .Where(f => f.DeclaredAccessibility == Accessibility.Public && !f.IsStatic && !f.IsConst))
                {
                    if (HasAttribute(field, typeof(Popcorn.AlwaysAttribute).FullName) || 
                        HasAttribute(field, typeof(Popcorn.DefaultAttribute).FullName))
                    {
                        hasAlwaysOrDefaultAttribute = true;
                        break;
                    }
                }
            }
            
            // Process properties
            foreach (var property in targetType.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.GetMethod != null && !p.IsIndexer))
            {
                if (!ShouldSerializeMember(property))
                {
                    continue;
                }
                
                AddMemberSerializationCode(property, property.Type, property.Name, false, propertySerializationCode, context, allTypeNames, hasAlwaysOrDefaultAttribute);
            }
            
            // Process fields
            foreach (var field in targetType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => f.DeclaredAccessibility == Accessibility.Public && !f.IsStatic && !f.IsConst))
            {
                if (!ShouldSerializeMember(field))
                {
                    continue;
                }
                
                AddMemberSerializationCode(field, field.Type, field.Name, true, propertySerializationCode, context, allTypeNames, hasAlwaysOrDefaultAttribute);
            }
            
            var internalSerializationCode = $@"
                Func<string, string> naming = options.PropertyNamingPolicy != null ? options.PropertyNamingPolicy.ConvertName : (a) => a;
                var properties = value.PropertyReferences;
                var useAll = properties.Any(p => ""!all"".AsSpan().Equals(p.Name.Span, StringComparison.Ordinal));
                // Use default includes if either nothing is explicitly called out or if defaults are explicitly requested
                // or if no properties/fields have Always/Default attributes
                var useDefault = !properties.Any() || properties.Any(p => ""!default"".AsSpan().Equals(p.Name.Span, StringComparison.Ordinal));

                writer.WriteStartObject();
            
                {propertySerializationCode}

                writer.WriteEndObject();
            ";
            return internalSerializationCode;
        }

        private static void AddMemberSerializationCode(
            ISymbol member, 
            ITypeSymbol memberType, 
            string memberName, 
            bool isField,
            StringBuilder codeBuilder, 
            SourceProductionContext context, 
            HashSet<string> allTypeNames,
            bool hasAlwaysOrDefaultAttribute)
        {
            var serializedName = GetSerializedName(member);
            var originalName = memberName;
            var referenceName = $"value.Data.{originalName}";
            var serializeLine = $"JsonSerializer.Serialize(writer, {referenceName}, options);";
            
            var memberTypeName = memberType.ToDisplayString().Replace("?", "");
            var handleNullable = IsNullableType(memberType);
            Show($"Will Render {memberTypeName} {(isField ? "field" : "property")}", context);
            
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
                        writer.WriteStartArray();
                        foreach(var item in {referenceName})
                        {{
                            Pop{NameType(elementNamedType)}(
                                writer, 
                                new global::Popcorn.Shared.Pop<{arrayType.ElementType.ToDisplayString()}> {{ 
                                    Data = item, 
                                    PropertyReferences = propertyReference?.Children ?? global::Popcorn.Shared.PropertyReference.Default
                                }}, options);
                        }}  
                        writer.WriteEndArray();
                    }}";
                }
                else
                {
                    // Just serialize the array normally
                    serializeLine = $"JsonSerializer.Serialize(writer, {referenceName}, options);";
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
                    Pop{NameType(memberNamedType)}(
                        writer, 
                        new global::Popcorn.Shared.Pop<{memberType.ToDisplayString()}> 
                        {{ 
                            Data = ({memberType.ToDisplayString()}){referenceName}, 
                            PropertyReferences = propertyReference?.Children ?? global::Popcorn.Shared.PropertyReference.Default
                        }}, 
                        options); 
                }}";
            }
            
            var serializeGroup = "";
            
            // Is this _Always_ included? Then include it.
            if (HasAttribute(member, typeof(Popcorn.AlwaysAttribute).FullName))
            {
                serializeGroup = ($@"
            if(propertyReference == null || propertyReference.Negated == false)
            {{
                // {memberTypeName} {originalName} ({(isField ? "field" : "property")})
                writer.WritePropertyName(naming(""{serializedName}""));
                {serializeLine}
            }}");
            }
            // Is this included by !default Then include it unless excluded
            else if (HasAttribute(member, typeof(Popcorn.DefaultAttribute).FullName) || (!hasAlwaysOrDefaultAttribute && !HasAttribute(member, typeof(Popcorn.NeverAttribute).FullName)))
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
            // Find if this specific member is requested
            var propertyReference = properties.FirstOrDefault(p => ""{serializedName}"".AsSpan().Equals(p.Name.Span, StringComparison.Ordinal));
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
