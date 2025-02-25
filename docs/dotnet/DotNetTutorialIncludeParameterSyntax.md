# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Include Parameter Syntax

[Table Of Contents](../../docs/TableOfContents.md)

Popcorn's core feature is the ability to selectively include properties in API responses using the `include` parameter. This tutorial explains the syntax for the `include` parameter, including special references and property negation.

## Basic Include Syntax

The basic syntax for the `include` parameter is a comma-separated list of property names enclosed in square brackets:

```
?include=[Property1,Property2,Property3]
```

This will include only the specified properties in the response. For example:

```
?include=[FirstName,LastName]
```

Will return:

```json
{
  "FirstName": "John",
  "LastName": "Doe"
}
```

## Special References

Popcorn supports special references that modify the default behavior of property inclusion:

### !all - Include All Properties

The `!all` special reference includes all properties of an object:

```
?include=[!all]
```

This will include all properties in the response, regardless of whether they are marked as default or not.

### !default - Include Default Properties

The `!default` special reference includes only properties marked with the `[Default]` attribute:

```
?include=[!default]
```

This will include only properties that have been marked with the `[Default]` attribute.

## Property Negation

You can negate the inclusion of specific properties by prefixing them with a `-` character:

### Negating Specific Properties

```
?include=[Property1,Property2,-Property3]
```

This will include `Property1` and `Property2`, but explicitly exclude `Property3`.

### Negating Properties with !all

```
?include=[!all,-Property1]
```

This will include all properties except `Property1`.

### Negating Properties with !default

```
?include=[!default,-DefaultProperty1]
```

This will include all default properties except `DefaultProperty1`.

## Nested Properties

You can also specify nested properties using nested square brackets:

```
?include=[Property1,NestedObject[NestedProperty1,NestedProperty2]]
```

This will include `Property1` and the nested properties `NestedProperty1` and `NestedProperty2` of the `NestedObject`.

## Case Sensitivity

Property names in the `include` parameter are case-sensitive. For example:

```
?include=[Name]
```

Will include a property named `Name`, but not a property named `name`.

## Examples

Here are some examples of valid include parameter syntax:

```
?include=[]                                          # Empty selection (defaults only)
?include=[FirstName,LastName]                        # Simple fields
?include=[MyProperty1,_ASecondProperty,_0]           # Numbers and underscores
?include=[FirstName,Child[FirstName,LastName],_Age]  # Nested entities
?include=[AllMyChildren[FirstName,LastName]]         # Collections
?include=[!all,-ExcludedProperty]                    # All properties except ExcludedProperty
?include=[!default,-DefaultProperty]                 # Default properties except DefaultProperty
?include=[Property1,Property2,-Property3]            # Include Property1 and Property2, exclude Property3
```

## Invalid Syntax

Here are some examples of invalid include parameter syntax:

```
?include=[1One]              # Starts with number
?include=[Property!Name]     # Contains punctuation
?include=[A]                 # Single character
?include=[___]               # Only underscores
?include=[!Property]         # Invalid negation syntax (use -Property instead)
```

## Attribute Interaction

The include parameter interacts with Popcorn's attribute system:

- `[Always]` - Properties with this attribute are always included, regardless of the include parameter
- `[Default]` - Properties with this attribute are included by default when no specific properties are requested
- `[Never]` - Properties with this attribute are never included, regardless of the include parameter

For example, even if you try to negate a property with the `[Always]` attribute, it will still be included:

```
?include=[-AlwaysIncludedProperty]  # AlwaysIncludedProperty will still be included
```

Similarly, if you try to include a property with the `[Never]` attribute, it will not be included:

```
?include=[NeverIncludedProperty]  # NeverIncludedProperty will not be included
