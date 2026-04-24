# Migration Analysis: Reflection → Source Generator

## Key Finding
Zero legacy features are genuine technical non-starters under source generation, **except one edge case**: truly polymorphic serialization of a type discovered at runtime whose members the trimmer has stripped. Every other feature is portable; what changes is the **configuration API surface**, not the semantics.

## The Pattern
Every legacy feature that uses **runtime lambdas for configuration** (`.Translate(e => e.FirstName + " " + e.LastName)`, `.Authorize<Car>((s,c,v) => ...)`, `.SetInspector((d,c,e) => ...)`) is incompatible with source generation *as designed*, because the generator needs its inputs at build time. Features that get carried forward remap to one of:
1. An attribute pointing to a static/partial method → generator emits a direct call at the callsite.
2. A DI-registered interface implementation → generator emits a DI resolution call at the callsite.

Both options are AOT- and trim-safe. The second is more idiomatic modern ASP.NET Core.

## Scope Decision (resolved)

Four legacy features were technically feasible under source generation but are **explicitly dropped from v2 scope**:

- **Sorting** — never used in practice; query-param endpoints typically implement sort themselves.
- **Pagination** — same; endpoints handle `Skip/Take` and page metadata directly when needed.
- **Filtering** — same; endpoints own their filter grammar.
- **Authorizers** — same; authorization is handled by standard ASP.NET authorization middleware and endpoint-level checks.

The complexity of dispatching these through the generator (switch-based type-driven dispatch, DI caching per request, middleware coordination) was not justified by actual usage. Callers who genuinely need these behaviors implement them at the endpoint level with the tools modern ASP.NET already provides.

This decision resolves the "scope decision required before merge" question that previously gated the spike.

## Confirmed Feasibility (by feature, post-scope-decision)

### Already supported by construction
- **Lazy loading.** Generator never touches properties outside the include list; EF lazy-load triggers are naturally avoided.
- **Blind expansion for user-declared types.** The generator's `GetReferencedTypes` BFS walks every reachable type from `[JsonSerializable(typeof(ApiResponse<T>))]`. Users get the "just declare the type, it works" experience without explicit `Map<>()` config.
- **Optional property `?` prefix.** Unknown include names are silently skipped — no `UnknownMappingException` equivalent in the generated path.

### Doable with attribute-driven design (Tier 1/2)
- **Sub-property defaults** (Tier 1). `[SubPropertyDefault("[Make,Model]")]` attribute read at build time.
- **Custom response envelope** (Tier 1). Marker-attribute design: `[PopcornEnvelope]` on the type + `[PopcornPayload]` / `[PopcornError]` / `[PopcornSuccess]` on the slot properties. Generator emits typed `CreateSuccess` / `CreateError` factories keyed by envelope type.
- **(Dropped 2026-04-23)** `ExpandFrom` projections. Technically doable via a generator-emitted `ProjectionType.From(Source)` copy method, but the v7 `MapEntityFramework` pattern it would replace was an *interception* feature (serializer sees `S`, emits `P`), not a factory — so `[ExpandFrom]` wasn't clean parity. The three real use cases have cleaner answers: `[Never]` on internal source properties, a hand-written factory, or `Mapster.SourceGenerator` for complex mapping. See `docs/MigrationV7toV8.md` §7.

### Doable via DI registration (Tier 2)
- **(Dropped 2026-04-23)** Blind handlers for external types. Standard `System.Text.Json` `JsonConverter<T>` registered on `JsonSerializerOptions.Converters` covers this cleanly and composes with Popcorn transparently (Popcorn's generator falls through to `JsonSerializer.Serialize` for unknown types; STJ picks up the registered converter). See `docs/MigrationV7toV8.md` §8.

### Doable via middleware (Tier 1)
- **Exception → envelope rewriting.** `UsePopcornExceptionHandler()` catches unhandled exceptions, looks up the configured envelope type via `PopcornOptions`, and writes an `ApiError`-populated envelope. Envelope **shape** is source-gen; exception **handling** is middleware. Clean split.

### Doable via attribute-tagged methods (Tier 2)
- **Translators (computed properties).** C# computed property (`public string FullName => First + " " + Last;`) works today, zero framework. 3 passing tests.
- **(Dropped 2026-04-23)** Translators with DI. The DI-during-serialization pattern is an antipattern (N+1, hidden I/O, scope threading). The clean answer is endpoint-side resolution: resolve services in the route handler, populate the DTO, serialize. See `docs/MigrationV7toV8.md` §5.
- **Factories.** Moot for v2.0 (write-only). When deserialization ships, `[Factory]`-tagged static method.

### Genuine non-starter
- **Polymorphic unknown-at-build-time types.** A reflection-based expander can walk any concrete runtime type by name; the trimmer will have removed the metadata. There is no AOT-compatible answer here. Mitigations: document the requirement to register all expected runtime types via `[JsonSerializable]`, and provide generator diagnostics that fire when a property's declared type is `object`/abstract-without-registered-derived-types.

### Moot (superseded, not non-starter)
- **Contexts as `Dictionary<string,object>`.** Legacy `SetContext(dict)` passed ambient data into lambdas. Under v2, translator methods receive DI services directly as parameters. Same capability, cleaner shape. The dictionary concept is deleted entirely.

## Feature Tiers for v2.0 Merge

**Tier 1 — MUST ship with v2.0 (core parity):**
- Custom envelope + exception middleware (error handling parity)
- `[SubPropertyDefault]` (common include ergonomics)

**Tier 2 — cleared 2026-04-23.** All three planned items (`[ExpandFrom]`, `[Translator]` with DI, `IPopcornBlindHandler<TFrom,TTo>`) were dropped after use-case analysis showed each has a cleaner answer using patterns already native to ASP.NET Core + STJ. Documented replacements in `docs/MigrationV7toV8.md` §5/§7/§8. Polymorphism dispatch via `[JsonDerivedType]` remains Tier-2-deferred if a consumer asks.

**Tier 3 — DEFER to v2.x or drop:**
- Factories (moot until deserialization).
- Deserialization (out of scope).
- Legacy `Dictionary<string,object>` context (dropped).

**Dropped from v2 scope:**
- Sorting, filtering, pagination, authorizers — see "Scope Decision" above.

## What We Learned About Source Generator Constraints
1. **Build-time inputs only.** Anything the user wants to inject must be visible in source: attributes, type references, method signatures. Lambdas, runtime `Type` objects, dynamically-built expressions are off-limits.
2. **Reflection-free emitted code.** Generated converters must compile to direct property accesses (`writer.WriteString("Name", source.Name)`) — no `typeof(T).GetProperty(...)`.
3. **Trim safety is a free byproduct.** Because emitted code references every type/property it touches, the linker keeps them. No special `[DynamicallyAccessedMembers]` plumbing needed.
4. **DI is the extensibility escape hatch.** When the user needs to inject behavior (translation, external-type handling), DI resolution at the callsite is the source-gen-friendly replacement for configuration lambdas. Idiomatic for modern .NET.
5. **Netstandard2.0 generator target is a real constraint.** Generator code itself can't use `Span<T>`, `init`, file-scoped namespaces, records, etc. — user models can, but the generator's `ExpanderGenerator.cs` cannot.
