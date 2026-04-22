# Migration Analysis: Reflection → Source Generator

## Key Finding
Zero legacy features are genuine technical non-starters under source generation, **except one edge case**: truly polymorphic serialization of a type discovered at runtime whose members the trimmer has stripped. Every other feature is portable; what changes is the **configuration API surface**, not the semantics.

## The Pattern
Every legacy feature that uses **runtime lambdas for configuration** (`.Translate(e => e.FirstName + " " + e.LastName)`, `.Authorize<Car>((s,c,v) => ...)`, `.SetInspector((d,c,e) => ...)`) is incompatible with source generation *as designed*, because the generator needs its inputs at build time. These features remap to one of:
1. An attribute pointing to a static/partial method → generator emits a direct call at the callsite.
2. A DI-registered interface implementation → generator emits a DI resolution call at the callsite.

Both options are AOT- and trim-safe. The second is more idiomatic modern ASP.NET Core.

## Confirmed Feasibility (by feature)

### Already supported by construction
- **Lazy loading.** Generator never touches properties outside the include list; EF lazy-load triggers are naturally avoided.
- **Blind expansion for user-declared types.** The generator's `GetReferencedTypes` BFS walks every reachable type from `[JsonSerializable(typeof(ApiResponse<T>))]`. Users get the "just declare the type, it works" experience without explicit `Map<>()` config.
- **Optional property `?` prefix.** Unknown include names are silently skipped — no `UnknownMappingException` equivalent in the generated path. Test needed, not code change.

### Doable with attribute-driven design
- **Sorting.** Set of sortable properties is known at build time. Generator emits a `switch(propertyName) => typed comparator` dispatch. Opt-in via `[Sortable]`. Likely faster than the reflection version.
- **Filtering.** Same pattern as sorting. `[Filterable(FilterOps)]` attribute; generator emits per-property typed predicate. Grammar stays simple (`[Field:op:value]`) to avoid AOT-hostile expression evaluation.
- **Pagination.** Not a serializer concern. Middleware parses `?page=&pageSize=`, applies `Skip/Take`, emits `PageInfo` into the envelope.
- **Sub-property defaults.** `[SubPropertyDefault("[Make,Model]")]` attribute read at build time.
- **`ExpandFrom` projections.** `[ExpandFrom(typeof(Source))]` on projection class; generator emits copy logic.
- **Custom response envelope.** `[PopcornEnvelope]` on a user type; generator wraps `Pop<T>` inside it.

### Doable via DI registration
- **Authorizers.** `IPopcornAuthorizer<T>` registered once per type. Generator emits `serviceProvider.GetService<IPopcornAuthorizer<T>>()?.AuthorizeItem(item)` gates during enumeration and `AuthorizeInclude(source, name, value)` gates during property emission.
- **Blind handlers (external types).** `IPopcornBlindHandler<TFrom,TTo>` registered per-type-pair. Generator sees `TFrom` during walk; if a handler exists, emits conversion call.
- **Inspectors (exception wrapping).** Standard ASP.NET `UseExceptionHandler`-style middleware that writes the configured envelope with `Success=false` + `Error` populated. Envelope **shape** is source-gen; exception **handling** is middleware. Clean split.

### Doable via attribute-tagged methods
- **Translators (computed properties).** Simplest case: just use a C# computed property (`public string FullName => First + " " + Last;`) — works today, zero framework. Complex case: `[Translator(nameof(Owner))] public static EmployeeRef ResolveOwner(Car c, IEmployeeLookup svc)` with generator-emitted DI resolution.
- **Factories.** Moot for v2.0 (write-only). When deserialization ships, `[Factory]`-tagged static method.

### Genuine non-starter
- **Polymorphic unknown-at-build-time types.** A reflection-based expander can walk any concrete runtime type by name; the trimmer will have removed the metadata. There is no AOT-compatible answer here. Mitigations: document the requirement to register all expected runtime types via `[JsonSerializable]`, and provide generator diagnostics that fire when a property's declared type is `object`/abstract-without-registered-derived-types.

### Moot (superseded, not non-starter)
- **Contexts as `Dictionary<string,object>`.** Legacy `SetContext(dict)` passed ambient data into lambdas. Under v2, translator/authorizer methods receive DI services directly as parameters. Same capability, cleaner shape. The dictionary concept is deleted entirely.

## Scope Decision Required Before Merge
The spike currently ships **roughly 35-40% of the legacy feature surface**. Shipping as-is would regress consumers who depend on sorting, authorization, pagination, inspectors, advanced projections, or custom envelopes. Suggested tiers:

**Tier 1 — MUST ship with v2.0 (core parity):**
- Sorting, filtering, pagination (common API table-stakes)
- Authorizers (security)
- Custom envelope + exception middleware (error handling parity)
- `[SubPropertyDefault]` (common include ergonomics)

**Tier 2 — SHOULD ship with v2.0 or soon after:**
- `[Translator]` methods with DI (computed values needing services)
- `IPopcornBlindHandler<TFrom,TTo>` (external types like Geometry)
- `[ExpandFrom]` projection attribute

**Tier 3 — DEFER to v2.x or drop:**
- Factories (moot until deserialization)
- Deserialization (out of scope)
- Legacy `Dictionary<string,object>` context (dropped)

## What We Learned About Source Generator Constraints
1. **Build-time inputs only.** Anything the user wants to inject must be visible in source: attributes, type references, method signatures. Lambdas, runtime `Type` objects, dynamically-built expressions are off-limits.
2. **Reflection-free emitted code.** Generated converters must compile to direct property accesses (`writer.WriteString("Name", source.Name)`) — no `typeof(T).GetProperty(...)`.
3. **Trim safety is a free byproduct.** Because emitted code references every type/property it touches, the linker keeps them. No special `[DynamicallyAccessedMembers]` plumbing needed.
4. **DI is the extensibility escape hatch.** When the user needs to inject behavior (auth, translation, external-type handling), DI resolution at the callsite is the source-gen-friendly replacement for configuration lambdas. Idiomatic for modern .NET.
5. **Netstandard2.0 generator target is a real constraint.** Generator code itself can't use `Span<T>`, `init`, file-scoped namespaces, records, etc. — user models can, but the generator's `ExpanderGenerator.cs` cannot.
