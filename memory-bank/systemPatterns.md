# System Patterns: Popcorn

## Architecture

### Runtime pipeline (new, source-generated path)
```
Client → HTTP GET ?include=[...] → ASP.NET pipeline
  → PopcornAccessor parses include → PropertyReference[] (cached per request)
  → Endpoint returns ApiResponse<T> (wraps Pop<T> = { Data, PropertyReferences })
  → System.Text.Json uses generated JsonConverter<T>
  → Converter walks properties, applies attributes + PropertyReferences, writes only selected fields
```

### Build-time pipeline
```
User's JsonSerializerContext subclass with [JsonSerializable(typeof(ApiResponse<T>))] attrs
  → ExpanderGenerator (IIncrementalGenerator)
    ├ finds JsonSerializerContext classes
    ├ filters JsonSerializable attrs whose T inherits ApiResponse<>
    ├ walks referenced types transitively (GetReferencedTypes)
    └ emits <Type>JsonConverter.g.cs per type + RegisterConverters.g.cs
  → User calls options.AddPopcornOptions() to register them all
```

## Key Components

### `Popcorn.Shared` (runtime, netstandard2.0)
- `ApiResponse<T>` — response envelope, implicit-converts from `Pop<T>`.
- `Pop<T>` — `{ Data: T, PropertyReferences: IReadOnlyList<PropertyReference> }`.
- `PropertyReference` — parsed include node: `Name`, `Negated`, `Children`. Parser in `ParseIncludeStatement`.
- `AlwaysAttribute`, `NeverAttribute`, `DefaultAttribute` — in `Popcorn` namespace, on properties.
- `IPopcornAccessor` / `PopcornAccessor` — scoped service, reads `include` from current `HttpContext.Request.Query`, caches parse result, exposes `CreateResponse<T>(T)`.
- `HttpContextExtensions.Respond<T>`, `ServiceCollectionExtensions.AddPopcorn()`.

### `Popcorn.SourceGenerator` (build-time, netstandard2.0, `IsRoslynComponent`)
- `ExpanderGenerator : IIncrementalGenerator` — single entry point.
- Type classification via three hashsets: `NumberTypes`, `StringTypes`, `BoolTypes`, plus `IgnoreTypes` (char, float, double, Guid, DateTime, TimeSpan, DateTimeOffset — written verbatim, not expanded).
- Collection handling: detects `IEnumerable<T>` and `IDictionary<K,V>` via `InheritsOrImplements`. For dictionaries only the value type is walked; keys are written as-is.
- Circular reference handling: generator emits runtime code that tracks visited objects in a `HashSet<object>` and writes `{"$ref":"circular"}` on recursion.

## Include Syntax

### Grammar
- `[]` — empty (treated as default).
- `[Field1,Field2]` — explicit list.
- `[Field[Nested]]` — recursion.
- `[Collection[Field]]` — per-item selection.
- `!all`, `!default` — keywords.
- `-Field` — negation (exclusion) prefix.

### Valid names
- Regex: `[A-Za-z_][A-Za-z0-9_]*[A-Za-z0-9]+[A-Za-z0-9_]*` — starts with letter/underscore, min 2 chars, at least one non-underscore, alphanumeric + underscore only.

### Name resolution contract
Include and negation names are matched against the **wire name**, not the C# identifier. The wire name is:
- The argument to `[JsonPropertyName("...")]` if present on the property.
- Otherwise, the C# property name (subject to any `JsonNamingPolicy` on the options).

This is load-bearing: the `?include=` list is part of the public API contract, seen by API clients who have no access to the server's source. A client looking at a response body `{"display_name":"..."}` requests that field as `?include=[display_name]`. Requesting `[DisplayName]` (the C# name) is treated as an unknown name and produces no output for that field. Same rule applies to negation: `-display_name` is valid; `-DisplayName` is an unknown-name no-op.

### Combinations
- `[!all,-Secret]` — everything except Secret.
- `[!default,-Foo]` — default set minus Foo.
- `[Id,Name,-Email]` — explicit list with exclusion (exclusion applies when combined with a keyword like `!all`; plain `-Field` in a specific list is unusual but tolerated).

## Attribute Semantics
- `[Always]` — emitted regardless of include list; cannot be negated.
- `[Default]` — emitted when include is empty or `!default`; can be negated with `-Name`.
- `[Never]` — never emitted, even if explicitly requested.
- No attribute + no `[Default]` anywhere on the type → all properties default-included (implicit-all).
- No attribute + `[Default]` present on the type → only `[Default]`/`[Always]` members default-included.
- Conflicts (e.g. `[Always]` + `[Never]`) are not valid; detection not currently enforced (no conflicts observed in test corpus).

## Discovery Contract (provider-side API)
To opt a type into Popcorn serialization:
1. Put the model in a project that references `Popcorn.Shared` and adds `Popcorn.SourceGenerator` as an analyzer.
2. Declare a `partial class` inheriting `JsonSerializerContext`.
3. Add `[JsonSerializable(typeof(ApiResponse<YourType>))]` attributes for each top-level response type. The generator walks nested types automatically.
4. Register at startup:
   ```csharp
   builder.Services.AddHttpContextAccessor();
   builder.Services.AddPopcorn();
   builder.Services.ConfigureHttpJsonOptions(o => {
       o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
       o.SerializerOptions.AddPopcornOptions(); // registers generated converters
   });
   ```
5. Return `contextAccess.CreateResponse(data)` from endpoints (endpoints receive `IPopcornAccessor` via DI).

## Future/Planned Transport
- Header-based include via `POPCORN-INCLUDE` header — not implemented. Would remove URL length limits and clean up URLs for POST/PUT.

## Deferred or Out-of-Scope for This Spike
- **Deferred (intend to port from legacy)**: sorting, pagination, filtering, authorization, response inspectors, contexts, lazy loading, blind expansion.
- **Out of scope**: deserialization (generated converters are write-only), runtime-configurable projection API, multi-target (non-.NET) providers.

## Error Surfaces
- Generator diagnostic `JSG001` — source generation failure (emitted at build time, doesn't fail build catastrophically).
- Invalid include strings → `PropertyReference.Default` fallback.
- Missing `JsonSerializable` attr for a type at runtime → standard `System.Text.Json` "no metadata" error.
