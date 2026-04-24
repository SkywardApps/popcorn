# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Wildcard Includes

[Table Of Contents](../../docs/TableOfContents.md)

Popcorn's `?include=` grammar supports two wildcard keywords: `!all` and `!default`. Both are
prefixed with `!` (not `-` — that prefix is reserved for [negation](DotNetTutorialIncludeParameterSyntax.md)).

> **v7 → v8 note.** In v7 you could use `*` as a wildcard (`?include=[*]`). In v8 the wildcard
> is **`!all`**. If you're migrating a client, update the URLs.

## `!all` — every available field

`!all` includes every property on the target type (minus anything marked `[Never]`, which still
wins). Useful for admin screens that need the full record without spelling out every field.

Model:

```csharp
using Popcorn;

public class Employee
{
    [Default] public string FirstName { get; set; } = "";
    [Default] public string LastName  { get; set; } = "";

    public DateTimeOffset Birthday { get; set; }
    public int VacationDays { get; set; }

    [Never] public string SocialSecurityNumber { get; set; } = "";

    public List<Car> Vehicles { get; set; } = new();
}
```

Bare request (returns default set):

```
GET /employees
→ { FirstName, LastName }
```

Wildcard request:

```
GET /employees?include=[!all]
```

```json
{
  "Success": true,
  "Data": [
    {
      "FirstName": "Liz",
      "LastName": "Lemon",
      "Birthday": "1981-05-01T00:00:00+00:00",
      "VacationDays": 0,
      "Vehicles": [...]
    },
    { "FirstName": "Jack", "LastName": "Donaghy", ... }
  ]
}
```

`SocialSecurityNumber` is still absent — `[Never]` beats `!all`. That's the point: `[Never]`
is the one attribute whose guarantee is load-bearing for security.

## `!default` — the configured default set

`!default` is the default set explicitly invoked. It's only interesting in combination with
other include tokens, since a bare request already yields the default set:

```
GET /employees?include=[!default,Birthday]
→ FirstName, LastName, and Birthday
```

Handy when you want "the default set plus one extra field" without respelling the defaults.

## Negation: `!all,-Field` and `!default,-Field`

Both wildcards combine with `-FieldName` to drop specific fields:

```
GET /employees?include=[!all,-Birthday]
→ everything except Birthday

GET /employees?include=[!default,-LastName]
→ default set without LastName
```

Negating an `[Always]`-marked field is a silent no-op — `[Always]` wins.

## Wildcards on nested sub-entities

Wildcards also work inside nested brackets:

```
GET /employees?include=[FirstName,Vehicles[!all]]
```

```json
{
  "Success": true,
  "Data": [
    {
      "FirstName": "Liz",
      "Vehicles": [
        { "Make": "Pontiac", "Model": "Firebird", "Year": 1981, "Color": 2 }
      ]
    },
    ...
  ]
}
```

This is particularly useful when the parent has a tight default set but you want to walk the
full child shape.

## Unknown field names are silently ignored

Typos or fields that don't exist on the target type are treated as no-ops:

```
GET /employees?include=[FirstName,Whoops]
→ { FirstName }
```

This is intentional — clients evolve at a different cadence from servers, and a 500 on a
renamed field breaks more things than it helps. Names must resolve to the **wire name** (the
`[JsonPropertyName("…")]` argument if present, otherwise the C#-name normalized by your
`JsonNamingPolicy`). See [Include Parameter Syntax](DotNetTutorialIncludeParameterSyntax.md)
for the full contract.

## See also

- [Include Parameter Syntax](DotNetTutorialIncludeParameterSyntax.md) — the complete grammar.
- [Default Includes](DotNetTutorialDefaultIncludes.md) — shaping what the default set is.
- [Internal-Only Fields](DotNetTutorialInternalOnly.md) — `[Never]` and why wildcards can't override it.
