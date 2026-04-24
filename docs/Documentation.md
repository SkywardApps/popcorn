# [Popcorn](../README.md) > Documentation

[Table Of Contents](TableOfContents.md)

This document contains protocol level documentation -- that is, the contract defined with the API consumer, regardless of platform-specific implementation.

Platform-specific implementation documentation can be found here:
+ [DotNet](dotnet/DotNetDocumentation.md)
 
<a name="includedFields"/>

## Declaring included fields
There are intended to be two main mechanisms for specifying which fields are to be included in a query response:
1. A query string appended to the url, 'include'.
2. A custom header attached to the request, 'POPCORN-INCLUDE'

Currently only the query string mechanism is supported in the .NET provider. Header-based
transport is on the roadmap — see [roadmap.md](../roadmap.md).

### Names are wire names, not C# names

This is protocol-level, and it matters for any client: the names in an `?include=` list are
matched against the **wire name** of each field — the name the client actually sees in
response bodies. In the .NET provider this is whichever comes first:

1. The value of `[JsonPropertyName("...")]` on the property, if present.
2. The C# property name, normalized by any `JsonNamingPolicy` the server has configured
   (CamelCase, SnakeCase, etc.).

A client that sees `{"display_name": "..."}` in responses should request that field as
`?include=[display_name]`. Requesting `?include=[DisplayName]` is silently treated as an
unknown name and the field will not be emitted. Same rule applies to negation.

<a name="includingFields"/>

## Including Fields
Fields shall be listed, in any order, as a comma delimited list surrounded by square brackets `[` and `]`.  The list shall start with an open bracket, contain zero or more valid property names, and terminate with a close bracket.

Valid field names shall be any characters matching the regular expression `/[A-Za-z_][A-Za-z0-9_]*[A-Za-z0-9]+[A-Za-z0-9_]*/`.
This basically means a simple name that starts with an alphabetical or underscore (not a number), then contains at least one non-underscore.
The minimum field name length is two characters.

### Examples
Acceptable formats:

No properties referenced: `[]`  
Properties with simple names: `[FirstName,LastName]`  
Properties with numbers and underscores: `[MyProperty1,_ASecondProperty,_0]`  

Disallowed field names are:

Starting with a number: `1One`  
Punctuation: `Property!Name`  
Single character: `A`
Only underscores: `___`

<a name="includingSubEntities"/>

## Including Sub-entities
Sub-entities are referred to as traditional fields, and can be embedded by simply referencing the subentity field name.  Optionally, after the field name, an additional include list may be embedded in the larger call, 
listing field names of the subentity to include.  These declarations can be recursive, allowing as many nested field lists as needed to define the full scope of the desired response.

### Examples
Assuming a subentity field named 'Child':

`[Child]`

`[FirstName,Child]`

`[FirstName,Child[FirstName,LastName],_Age]`

`[Child[FirstName,GrandChild[FirstName]]]`

<a name="includingCollections"/>

## Including Collections
Collections shall behave as any other field.  If the collection is of a simple type, reference the field that contains the collection and the contents of the collection shall be returned.  If the collection is of a subentity type,
then an additional include list may be embedded in the larger call, listing field names of each subentity in the list to include.  As with direct sub-entities, these may be nested.

### Examples

`[ArrayOfNumbers]`

`[AllMyChildren]`

`[AllMyChildren[FirstName, LastName]]`

`[AllMyChildren[FirstName, AllGrandChildren[FirstName]]]`

<a name="defaultFields"/>

## Default fields
Each entity type shall define its own default fields.  These are the implicit fields that will be included in the case that:
+ No includes are specified at all
+ An empty set of includes are provided (ie, ```[]```)
+ The entity is included via a field reference in a parent object, but no subentity field list is provided or an empty list is provided. (Eg, ```[AllMyChildren]``` or ```[AllMyChildren[]]```)

### Wildcards and negation

Two keywords (both prefixed with `!`) let a client refer to the default and full field sets:

+ `!all` includes every available field on the target type.
+ `!default` is the default field set explicitly invoked — useful when combining with negation or an additional include.

A `-` prefix on a field name negates it (removes it from what would otherwise be emitted):

```
?include=[!all,-Secret]      # everything except Secret
?include=[!default,-Email]   # default set minus Email
?include=[Id,Name,Child[Id,-Secret]]  # explicit plus nested with negation
```

Implementation contract: fields tagged with an "always emit" attribute (e.g. `[Always]` in the
.NET provider) are emitted even under negation — negating an always-emit field is a silent
no-op. Conversely, fields tagged "never emit" (e.g. `[Never]`) are never emitted, even if
explicitly requested or covered by `!all`.

<a name="methods"/>

## Methods
While designed around resource querying and retrieval, this syntax can be utilized in POST or PUT methods in order to control what is returned as a result of the action.  For example, if you have an endpoint to POST a 'Car' entity to, you can 
indicate which fields are returned of the newly created car.  This may be particularly important if there are fields that are generated or populated on the client.

```javascript
POST "https://myserver.com/api/1/employee/5390/cars?include=[Id,Owner[Id,FirstName,LastName],Model]"
{
    "Color":"Red",
    "License":"AX3939",
    "Make":"Honda",
    "Model":"Fit"
}
```

The above will create a new car entity, assign it to employee 5390, then return the following...

```javascript
{
    "Id":102,
    "Owner":{
        "Id":5390,
        "FirstName":"Kenneth",
        "LastName":"Parcell",
    },
    "Model":"Fit"
}
```

...including a repeat of some of the data that we uploaded since we requested the Model back, but more importantly allowed us to get the Id that we generated for the car as well as the owner information.  This is a contrived example; in the real world
you probably knew the owner ahead of time (since we reference them by id) but demonstrates the ability to immediately access expanded data on an entity that was partially defined on the client.

<a name="movingForward"/>

## Moving forward
As indicated above, we would like to define an HTTP Header as an option to specify the field list.

Platform-specific implementations (providers) need to be created to adhere to the protocol.

Schema and/or documentation could be included in the protocol.

See [our Roadmap](Roadmap.md)


