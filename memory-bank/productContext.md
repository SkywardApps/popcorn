# Product Context: Popcorn

## Problem
REST APIs force a tradeoff between over-fetching (big payloads nobody reads) and under-fetching (N+1 round trips for related data). Mobile + low-bandwidth clients pay the cost. GraphQL solves it but replaces REST entirely. OData solves it but is heavy and .NET-centric.

## Solution
A thin protocol overlay on REST: the client names the fields it wants, including nested ones and collections, in a single query parameter. The server returns exactly that shape in one response.

```
GET /contacts?include=[Id,Name,PhoneNumbers[Number]]
```

## Functional Intent
1. **Field selection** — per-request shape control, no schema change.
2. **Single-request resolution** — related entities expand inline, recursively.
3. **Attribute-driven defaults** — server declares `[Always]` / `[Default]` / `[Never]` so a bare request still returns something sensible.
4. **Platform-agnostic** — protocol is JSON-over-HTTP; any language can provide a server or client implementation.

## Implementation Evolution
- **Legacy**: runtime reflection expander (`PopcornNetStandard`). Flexible but incompatible with AOT and trimming; allocates heavily on each request.
- **Current (this branch)**: build-time Roslyn source generator (`Popcorn.SourceGenerator`). AOT- and trim-safe. Type-safe. Generated `JsonConverter<T>` per registered type.

## UX Expectations

### API consumers (client devs)
- Human-readable include syntax: `[field,nested[field],-excluded,!all,!default]`.
- Visible in URL → cacheable, debuggable from a browser.
- One call replaces N calls for nested data.

### API providers (server devs)
- Drop-in middleware: `AddPopcorn()`, `AddPopcornOptions()` on the JSON options.
- Declare which types to support with `[JsonSerializable(typeof(ApiResponse<Foo>))]` on a `JsonSerializerContext`.
- Annotate models with `[Always]` / `[Default]` / `[Never]`. No runtime configuration API required for the basics.
- Compatible with `WebApplication.CreateSlimBuilder` and `PublishAot=True`.

## Target Audience
- .NET API developers shipping to AOT / trimmed / container environments where reflection-heavy libraries fail.
- Mobile and low-bandwidth client developers who need payload-shape control over an existing REST surface.
- Teams that want GraphQL's selective-fetch ergonomics without leaving REST.

## Market Positioning
- Open source (Skyward App Company).
- Complementary, not a replacement, for GraphQL/OData.
- Differentiator vs. legacy Popcorn: AOT-first, performance-focused, no runtime reflection.
