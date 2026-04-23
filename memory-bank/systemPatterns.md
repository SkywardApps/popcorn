# System Patterns: Popcorn

## Architecture

### Runtime pipeline (new, source-generated path)
```
Client → HTTP GET ?include=[...] → ASP.NET pipeline
  → (optional) UsePopcornExceptionHandler buffers the response body
  → PopcornAccessor parses include → PropertyReference[] (cached per request)
  → Endpoint returns ApiResponse<T> (or custom envelope) wrapping Pop<T> = { Data, PropertyReferences }
  → System.Text.Json uses generated JsonConverter<Pop<T>>
  → Converter walks properties, applies attributes + PropertyReferences, writes only selected fields
  → On exception: middleware consults PopcornOptions.EnvelopeType and writes a structured error envelope
    via the default shape or a generator-emitted writer registered in PopcornErrorWriterRegistry
```

### Build-time pipeline
```
User's JsonSerializerContext subclass with [JsonSerializable(typeof(ApiResponse<T>))] and/or
[JsonSerializable(typeof(MyEnvelope<T>))] attrs (MyEnvelope<T> decorated with [PopcornEnvelope])
  → ExpanderGenerator (IIncrementalGenerator)
    ├ finds JsonSerializerContext classes
    ├ filters JsonSerializable attrs whose T inherits ApiResponse<> OR carries [PopcornEnvelope]
    ├ walks referenced types transitively (GetReferencedTypes)
    ├ analyzes each envelope (AnalyzeEnvelope walks base chain for marker attrs)
    │   and emits diagnostics JSG003–JSG007 for malformed shapes
    └ emits <Type>JsonConverter.g.cs per type + RegisterConverters.g.cs (which provides
      AddPopcornOptions JSON-hook + AddPopcornEnvelopes DI-hook, and a per-envelope error writer)
  → User calls options.AddPopcornOptions() and/or services.AddPopcornEnvelopes() to register them
```

### Envelope dispatch architecture (custom envelope + exception middleware)
Custom envelopes are declared at build time (markers) and dispatched at runtime (registry + middleware).
The generator does everything statically; the middleware does nothing reflection-based.

```
Build time:
  MyEnvelope<T> with [PopcornEnvelope] + marker properties
    → Generator extracts slot names + walks Pop<T> payload type
    → Emits WriteCustomErrorEnvelope method with a `switch (envelopeType)` arm per envelope
    → Emits AddPopcornEnvelopes / AddPopcornOptions hooks that call
      PopcornErrorWriterRegistry.Register(WriteCustomErrorEnvelope)

Startup (AOT-friendly):
  services.AddPopcorn(o => o.EnvelopeType = typeof(MyEnvelope<>));
  services.AddPopcornEnvelopes();   // installs the writer at DI time
  app.UsePopcornExceptionHandler(); // buffers response, catches exceptions

Request path:
  Normal success: endpoint returns MyEnvelope<T> { Payload = Pop<T>{...} },
    generated Pop<T> converter renders include-filtered JSON inside user-named slots.
  Exception: middleware resolves PopcornOptions, calls PopcornErrorWriterRegistry.TryWrite
    with the configured envelope type + ApiError; generator-emitted writer picks the matching
    envelope arm and emits {slotName: false, errorSlot: {...}} using Utf8JsonWriter
    (naming-policy-applied, reflection-free).
```

Fallback: when `PopcornOptions.EnvelopeType == typeof(ApiResponse<>)` the middleware writes
the default shape directly without consulting the registry. Users who never configure a custom
envelope pay no generator cost for this path.

## Key Components

### `Popcorn.Shared` (runtime, netstandard2.0)
- `ApiResponse<T>` — default response envelope, implicit-converts from `Pop<T>`. Has `Success`, `Data: Pop<T>`, `Error: ApiError?`. Parameterless ctor is `internal` so external callers cannot construct a "success-with-no-data" shape; use the `Pop<T>` overload, the `PropertyReference` overload, or `FromError(ApiError)`.
- `ApiError(Code, Message, Detail?)` — structured error record emitted by the exception middleware.
- `Pop<T>` — `{ Data: T, PropertyReferences: IReadOnlyList<PropertyReference> }`.
- `PropertyReference` — parsed include node: `Name`, `Negated`, `Children`. Parser in `ParseIncludeStatement`.
- `AlwaysAttribute`, `NeverAttribute`, `DefaultAttribute` — on properties. In `Popcorn` namespace.
- `SubPropertyDefaultAttribute` — on properties or fields; `[SubPropertyDefault("[Make,Model]")]` declares the include list used when the property is included without explicit sub-children. Generator pre-parses the string once per process into a `private static readonly` field and substitutes it at nested-`Pop<T>` callsites; explicit sub-children override, `[Always]` / `[Never]` on the sub-type still win.
- `PopcornEnvelopeAttribute` (on class/struct) + `PopcornPayloadAttribute`, `PopcornErrorAttribute`, `PopcornSuccessAttribute` (on properties) — marker attributes that opt a user type into custom-envelope dispatch.
- `PopcornOptions` — `{ EnvelopeType (default typeof(ApiResponse<>)), DefaultNamingPolicy }`. Consumed by `UsePopcornExceptionHandler`.
- `PopcornErrorWriterRegistry` — process-global static, `Volatile`-guarded. `Register(writer)` installs the generator-emitted writer; `TryWrite(...)` is called by the middleware on the error path.
- `IPopcornAccessor` / `PopcornAccessor` — scoped service, reads `include` from current `HttpContext.Request.Query`, caches parse result, exposes `CreateResponse<T>(T)`.
- `ApplicationBuilderExtensions.UsePopcornExceptionHandler()` — middleware; buffers the response body (so exceptions during serialization can still be captured), strips stale `Content-Length`, preserves custom headers, applies `PopcornOptions.DefaultNamingPolicy` when writing the error envelope.
- `HttpContextExtensions.Respond<T>`, `ServiceCollectionExtensions.AddPopcorn()` (idempotent) / `AddPopcorn(Action<PopcornOptions>)`.

### `Popcorn.SourceGenerator` (build-time, netstandard2.0, `IsRoslynComponent`)
- `ExpanderGenerator : IIncrementalGenerator` — single entry point.
- Type classification via three hashsets: `NumberTypes`, `StringTypes`, `BoolTypes`, plus `IgnoreTypes` (char, float, double, Guid, DateTime, TimeSpan, DateTimeOffset — written verbatim, not expanded).
- Collection handling: detects `IEnumerable<T>` and `IDictionary<K,V>` via `InheritsOrImplements`. For dictionaries only the value type is walked; keys are written as-is.
- Circular reference handling: generator emits runtime code that tracks visited objects in a `HashSet<object>` and writes `{"$ref":"circular"}` on recursion.

### Load-bearing generator conventions (don't work around)
Several helpers in `ExpanderGenerator` are load-bearing — any future code that emits `Pop<T>` or adds to the `allTypeNames` set must route through them or the generator will silently re-introduce defects we've already fixed.

- **`TypeNameForPop(ITypeSymbol)`** — the ONLY way to render a type name inside `Pop<...>` (both at registered-signature sites and at every callsite). Uses a `SymbolDisplayFormat` without `IncludeNullableReferenceTypeModifier`, so NRT `?` on reference types is stripped (callsites and signatures converge) while `Nullable<T>` on value types is preserved (distinct CLR identity). Using `ToDisplayString()` directly re-introduces CS8620 "Argument of type 'Pop<T?>' cannot be used for parameter 'Pop<T>'" warnings across every consumer build.
- **`IsBlindSerializableType(ITypeSymbol)`** — the gate for "do we emit a `Pop<T>` body for this type?". Returns true for primitives, string, bool, `Guid`/`DateTime`/etc. (the `IgnoreTypes` set), enums, and `Nullable<T>` wrappers of any of those. Two usage sites:
  1. Filter `allTypeNames` when assembling it at the top of `GenerateJsonConverter` — blind types MUST NOT appear there, or every downstream list/dict/array converter that checks `allTypeNames.Contains(...)` will decide to emit a call to a `Pop<primitive>` method that never exists. One bad root-level `ApiResponse<int?>` registration used to cascade into compile errors across unrelated converters.
  2. Branch at the top of `GenerateJsonConverter` — if the target type itself is blind, emit `JsonSerializer.Serialize(writer, value.Data, options)` directly instead of trying to recurse.
- **`IsConverterCycleSafe(ITypeSymbol, HashSet<string>)`** — DFS through the payload type graph (unwrapping arrays / `IEnumerable<T>` / `IDictionary<K,V>` / `Nullable<T>` via `UnwrapPayloadType`) to determine if a converter's type can transitively reach itself. Only types in `allTypeNames` participate — blind types don't recurse through Popcorn. Cycle-safe converters pass `null` for `HashSet<object>? visitedObjects` at their entry point; cycle-risky converters keep allocating. Body ops are always null-conditional so both cases compose correctly when cycle-safe types are reached from cycle-risky parents.
- **`FlagSetupCode` constant + `Pop{X}Inner` split** — every complex-object converter emits two overloads: (a) `Pop{X}(writer, value, options, visitedObjects)` computes `useAll`/`useDefault`/`naming` via a one-pass scan of `PropertyReferences` then delegates to (b) `Pop{X}Inner(writer, value, options, visitedObjects, naming, useAll, useDefault)` which has the actual property-write body. List and dictionary converters pre-compute the flag setup once before their foreach loop and call `Pop{X}Inner` per item directly, skipping per-item rescans + naming-delegate allocations. Single-member callsites keep calling the 4-arg `Pop{X}` because each child's `propertyReference.Children` differs from the parent. `emitInnerOverload` is the branch flag in `GenerateJsonConverter` — set only for complex-object targets (collection/dict/blind/nullable-wrapper targets don't need the split).

### Constants that must match Roslyn's `ToDisplayString` format verbatim
`InheritsOrImplements` compares symbols via `OriginalDefinition.ToDisplayString()`, which emits generic argument lists with `", "` (comma + space). Type-name constants (`IEnumerableTypeName`, `IDictionaryTypeName`) MUST use that exact spacing. A no-space form silently fails the match and the type falls through to a less-specific dispatch branch — historically this is how `IDictionary<K,V>` and `ReadOnlyDictionary<K,V>` got routed through the IEnumerable path and emitted broken iterators.

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

### Discovery contract for a custom envelope + exception middleware
Additional steps when you want a non-default envelope shape and/or structured error handling:
1. Declare your envelope with marker attributes:
   ```csharp
   [PopcornEnvelope]
   public record class MyEnvelope<T>
   {
       [PopcornSuccess] public bool Ok { get; init; } = true;
       [PopcornPayload] public Pop<T> Payload { get; init; }
       [PopcornError]   public ApiError? Problem { get; init; }
   }
   ```
2. Register it as the app envelope (and wire the exception middleware):
   ```csharp
   builder.Services.AddPopcorn(o => {
       o.EnvelopeType = typeof(MyEnvelope<>);
       o.DefaultNamingPolicy = JsonNamingPolicy.CamelCase; // optional
   });
   builder.Services.AddPopcornEnvelopes(); // DI-time hook that registers the generator-emitted writer
   app.UsePopcornExceptionHandler();       // buffers body + catches exceptions
   ```
3. Return `new MyEnvelope<T> { Payload = new Pop<T>{ Data = model, PropertyReferences = accessor.PropertyReferences } }` from endpoints.

The generator enforces: exactly one `[PopcornPayload]` property (otherwise JSG003), unique markers (JSG004), `Pop<T>`-typed payload (JSG005 warning), `ApiError`-typed error (JSG006 warning), and no nesting inside a generic outer type (JSG007 — the C# open-generic `typeof(...)` expression can't be emitted for that case).

## Future/Planned Transport
- Header-based include via `POPCORN-INCLUDE` header — not implemented. Would remove URL length limits and clean up URLs for POST/PUT.

## Deferred or Out-of-Scope for This Spike
- **Dropped from v2 scope**: sorting, pagination, filtering, authorizers (never used in practice; see migrationAnalysis.md).
- **Deferred (intend to port from legacy)**: response inspectors (superseded by exception middleware + custom envelope), contexts (superseded by DI), lazy loading (supported by construction), blind expansion (superseded by IPopcornBlindHandler — Tier-2).
- **Out of scope**: deserialization (generated converters are write-only), runtime-configurable projection API, multi-target (non-.NET) providers.

## Middleware Constraints
- `UsePopcornExceptionHandler` buffers the entire response body in memory until the inner handler completes. This is correct for bounded JSON payloads (which Popcorn envelopes always are) but breaks streaming endpoints (SSE, chunked, long-polling). Scope the middleware to Popcorn routes; do not mount it globally if streaming endpoints coexist.
- Exceptions that occur after `Response.HasStarted == true` cannot be converted to an envelope (the client has already received a partial response). The middleware re-throws in that case, matching ASP.NET Core default behavior.

## Error Surfaces
- Generator diagnostic `JSG001` — source generation failure (emitted at build time, doesn't fail build catastrophically).
- Generator diagnostics `JSG003`–`JSG007` — malformed envelope: missing `[PopcornPayload]` (JSG003), duplicate marker (JSG004), non-`Pop<T>` payload (JSG005, warning), non-`ApiError` error slot (JSG006, warning), envelope nested inside a generic outer type (JSG007, warning — no valid open-generic `typeof` syntax possible).
- Invalid include strings → `PropertyReference.Default` fallback.
- Missing `JsonSerializable` attr for a type at runtime → standard `System.Text.Json` "no metadata" error.
- Unhandled exception in a handler → `UsePopcornExceptionHandler` rewrites the response as a structured envelope (default shape or custom via registry), status 500.
