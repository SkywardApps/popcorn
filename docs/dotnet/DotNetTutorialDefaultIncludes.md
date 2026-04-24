# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Default Includes

[Table Of Contents](../../docs/TableOfContents.md)

If you're new to Popcorn, start with [Getting Started](DotNetTutorialGettingStarted.md) —
this tutorial assumes you already have a working app and understand the basics of `?include=`.

Popcorn lets you declare which fields show up when the client makes a "bare" request — one
with no `?include=` parameter or with an empty `?include=[]`. You do this with three
attributes on the model, all in the `Popcorn` namespace.

| Attribute | Emitted when |
|---|---|
| `[Always]` | Every response, regardless of `?include=`. Cannot be negated. |
| `[Default]` | `?include=` is absent, empty, or `!default`. Can be negated via `-FieldName`. |
| `[Never]`  | Never. Not even when `?include=[FieldName]` asks for it explicitly. |

## Example model

Suppose we extend the `Employee` model from [Getting Started](DotNetTutorialGettingStarted.md)
with a computed `FullName` property:

```csharp
using Popcorn;

public class Employee
{
    public string FirstName { get; set; } = "";
    public string LastName  { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}";

    public DateTimeOffset Birthday { get; set; }
    public int VacationDays { get; set; }

    public List<Car> Vehicles { get; set; } = new();
}
```

A bare `GET /employees` request would return every public property. For most APIs that's too
much — the client ends up paying for data it won't render. Let's declare a smaller default.

## Controlling the default set with `[Default]`

```csharp
public class Employee
{
    [Default] public string FirstName { get; set; } = "";
    [Default] public string LastName  { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}";

    public DateTimeOffset Birthday { get; set; }
    public int VacationDays { get; set; }

    public List<Car> Vehicles { get; set; } = new();
}
```

```
GET /employees
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

The `Birthday`, `VacationDays`, `FullName`, and `Vehicles` fields are still available on the
wire — clients just have to ask for them:

```
GET /employees?include=[FirstName,Birthday,FullName]
```

```json
{
  "Success": true,
  "Data": [
    { "FirstName": "Liz",  "Birthday": "1981-05-01T00:00:00+00:00", "FullName": "Liz Lemon"   },
    { "FirstName": "Jack", "Birthday": "1957-07-12T00:00:00+00:00", "FullName": "Jack Donaghy" }
  ]
}
```

## The "no attributes" rule

If a type has **no** `[Default]` or `[Always]` attributes anywhere, every property is treated
as default-included. This keeps getting-started friction low — you can start emitting everything
and only introduce attributes when you want to tighten the default set.

| Attribute layout | Default set |
|---|---|
| No `[Default]` / `[Always]` anywhere on the type | All properties |
| At least one `[Default]` | Only properties marked `[Default]` or `[Always]` |
| `[Never]` on a property | That property is excluded, always, regardless of other rules |

Inheritance: `[Default]` / `[Always]` on a base class flow through to derived classes. `[Never]`
does the same. You don't need to re-declare them on the subclass.

## `[Always]` for fields that must be on every response

Some fields — primary keys, tenant IDs, version columns — should always be present even if the
client forgot to ask. `[Always]` guarantees that:

```csharp
public class Employee
{
    [Always]  public int Id { get; set; }
    [Default] public string FirstName { get; set; } = "";
    [Default] public string LastName  { get; set; } = "";
    ...
}
```

```
GET /employees?include=[FirstName]
```

```json
{
  "Success": true,
  "Data": [
    { "Id": 1, "FirstName": "Liz"  },
    { "Id": 2, "FirstName": "Jack" }
  ]
}
```

`Id` shows up even though the client didn't list it. Negation won't remove it either — a
request for `?include=[!all,-Id]` still emits `Id`. If a field is genuinely sensitive, use
`[Never]`, not `[Always]` — see [Internal-Only Fields](DotNetTutorialInternalOnly.md).

## Sub-property defaults via `[SubPropertyDefault]`

When a property is a complex type (like `List<Car>`), including the parent without specifying
sub-children normally falls back to the child type's own default set. `[SubPropertyDefault]`
lets you override that decision *for this property*:

```csharp
public class Employee
{
    [Default] public string FirstName { get; set; } = "";
    [Default] public string LastName  { get; set; } = "";

    [SubPropertyDefault("[Make,Model,Color]")]
    public List<Car> Vehicles { get; set; } = new();
}
```

```
GET /employees?include=[FirstName,Vehicles]
```

```json
{
  "Success": true,
  "Data": [
    {
      "FirstName": "Liz",
      "Vehicles": [
        { "Make": "Pontiac", "Model": "Firebird", "Color": 2 }
      ]
    },
    ...
  ]
}
```

Without the attribute, `Vehicles` would have emitted the `Car` type's own default set. The
override applies only when the client doesn't spell out sub-children: `?include=[Vehicles[Year]]`
still wins (explicit sub-children beat the attribute), and `[Never]` on a `Car` property still
wins over the attribute (declared "never-emit" always beats "default-emit").

The include string is parsed once per process into a static readonly field at generation time —
no per-request parsing cost.

## Negation and `!default` keyword

`?include=[!default]` is shorthand for "the default set" — useful when you want the default
set *plus* a couple of extra fields:

```
GET /employees?include=[!default,Birthday]
→ default set (FirstName + LastName) plus Birthday

GET /employees?include=[!default,-LastName]
→ default set minus LastName (FirstName only)
```

`[Always]`-marked fields are still included regardless of negation — `-Id` is a silent no-op
if `Id` is marked `[Always]`.

## See also

- [Include Parameter Syntax](DotNetTutorialIncludeParameterSyntax.md) for the full grammar
  (nesting, negation, `!all`).
- [Internal-Only Fields](DotNetTutorialInternalOnly.md) for `[Never]`.
- [Wildcard Includes](DotNetTutorialWildcardIncludes.md) for `!all`.
