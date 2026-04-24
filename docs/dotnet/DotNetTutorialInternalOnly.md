# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Internal-Only Fields (`[Never]`)

[Table Of Contents](../../docs/TableOfContents.md)

If you're new to Popcorn, start with [Getting Started](DotNetTutorialGettingStarted.md) first.

Sometimes a model carries fields the API client should never see — a database-internal row
version, a denormalized lookup key, a row-level secret. Popcorn gives you `[Never]`: a
compile-time guarantee that the marked field is dropped during serialization, no matter what
the client requests.

> **v7 → v8 note.** This attribute was called `[InternalOnly]` in v7. In v8 it's `[Never]`
> (and lives in the `Popcorn` namespace). Semantics are otherwise unchanged.

## Example

Say we add a Social Security Number to our `Employee` model. Clients should never receive it:

```csharp
using Popcorn;

public class Employee
{
    [Default] public string FirstName { get; set; } = "";
    [Default] public string LastName  { get; set; } = "";

    [Never] public string SocialSecurityNumber { get; set; } = "";
}
```

The field is still a normal C# property — internal code can read and write it. It just gets
dropped on the way out:

```
GET /employees?include=[FirstName,LastName,SocialSecurityNumber]
```

```json
{
  "Success": true,
  "Data": [
    { "FirstName": "Liz",  "LastName": "Lemon"   },
    { "FirstName": "Jack", "LastName": "Donaghy" }
  ]
}
```

Note that the request *asked* for `SocialSecurityNumber` and the server simply omitted it — no
error, no special handling. `[Never]` is stronger than "not in the default set"; it beats every
other inclusion mechanism: `?include=[!all]` won't emit it, `[SubPropertyDefault("[SSN]")]`
on the parent won't emit it, explicit `?include=[SocialSecurityNumber]` won't emit it.

## When to use `[Never]` vs omitting the field entirely

The safest way to keep a field off the wire is simply not to have it on the model the API
returns. Two situations where `[Never]` is the better answer:

1. **The model doubles as the internal data shape.** If your `Employee` class is also your
   domain entity, you can't drop `SocialSecurityNumber` — internal code needs it. `[Never]`
   lets the property stay in code and be invisible to the API.
2. **Defense in depth.** Even if you also use a separate DTO, adding `[Never]` to sensitive
   fields on the source model gives you an extra guardrail — if someone refactors and
   accidentally returns the domain entity directly, the field still stays off the wire.

If your API surface is naturally a separate DTO class and the source model never crosses the
API boundary, you can skip `[Never]` — just omit the property from the DTO.

## Why `[Never]` is better than "remove from projection"

In v7 users would often strip sensitive fields by leaving them off the projection class. That
worked, but it was easy to undermine accidentally (blind expansion would walk the source
object, or a future refactor added a "quick" path that returned the entity directly). v8
preserves `[Never]` explicitly at compile time because it's the one attribute whose guarantee
is load-bearing for security.

## Combining with `[SubPropertyDefault]`

`[Never]` takes precedence over `[SubPropertyDefault]`. Even if a parent's `[SubPropertyDefault]`
lists a `[Never]`-marked child property by name, the field stays hidden:

```csharp
public class Employee
{
    [SubPropertyDefault("[Make,Model,Color,InternalVin]")]  // mentions InternalVin explicitly
    public List<Car> Vehicles { get; set; } = new();
}

public class Car
{
    public string Make  { get; set; } = "";
    public string Model { get; set; } = "";
    public Colors Color { get; set; }

    [Never] public string InternalVin { get; set; } = "";
}
```

`GET /employees?include=[Vehicles]` emits `Make` + `Model` + `Color` per car and no `InternalVin`.
The substitution list is used to pick the default set; each child property still consults its
own attribute stack before being emitted.

## See also

- [Default Includes](DotNetTutorialDefaultIncludes.md) for `[Default]` and `[Always]`.
- [Include Parameter Syntax](DotNetTutorialIncludeParameterSyntax.md) for the full `?include=`
  grammar.
