# [Popcorn](../../README.md) > [Documentation](../Documentation.md) > [DotNet](DotNetDocumentation.md) > Tutorial: Lazy Loading

[Table Of Contents](../../docs/TableOfContents.md)

[Lazy loading](https://en.wikipedia.org/wiki/Lazy_loading) is the pattern of deferring the
materialization of related data until something actually asks for it. For web APIs, the
classic motivation is an `Author` list page that *could* cascade into every author's blog
posts but shouldn't, because nobody reads those posts on that page.

**In Popcorn v8, lazy loading works by construction — there is nothing to configure.**

## How

The source generator emits a `JsonConverter<T>` that writes one property at a time. If a
property isn't in the `?include=` list (or doesn't match the default set), the generator's
emitted code *never accesses the getter*. If that getter is a lazy-loaded EF navigation
property, Entity Framework's change tracker never fires a load query, and the database is
never touched.

This is a straight consequence of v8's architecture: there is no intermediate projection
step. The v7 reflection engine built a `Dictionary<string, object?>` describing the entire
output before serializing — which meant every getter ran, which triggered every navigation
load. v8 skips the dictionary and writes directly to `Utf8JsonWriter` property-by-property,
so untouched getters stay untouched.

## Example

```csharp
public class Author
{
    [Default] public int Id { get; set; }
    [Default] public string Name { get; set; } = "";

    // EF will lazy-load this when accessed.
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
}
```

```
GET /authors
→ default set: { Id, Name }. Posts getter is never called. Zero Post queries.

GET /authors?include=[Id,Name,Posts[Title]]
→ Posts getter runs per author; EF fires one Post query per author (configurable via eager loading).
```

If the client doesn't ask for `Posts`, Popcorn doesn't fetch `Posts`. No framework flags, no
`EnableLazyLoading(true)`, no `MapEntityFramework<S, P, Ctx>(...)` registration — the
generator's emission pattern is what gets you the behavior.

## What changed from v7

The v7 API had a `MapEntityFramework<TSource, TProjection, TContext>()` configuration method
that accepted an EF `DbContext` and wired up lazy-load support. **v8 has no equivalent** —
and doesn't need one. Drop `MapEntityFramework` calls from your startup configuration during
migration; everything continues to work.

See [MigrationV7toV8.md §7](../MigrationV7toV8.md#7-projections-replacing-mapentityframework)
for the full projection-pattern discussion.

## Caveats

- **Lazy loading requires EF proxies.** You still need to enable them at the EF level
  (`UseLazyLoadingProxies()` on the `DbContextOptionsBuilder`). Popcorn does not change how
  EF loads; it only changes which getters run.
- **Eager-loaded navigation properties are always materialized.** If your EF query uses
  `.Include(a => a.Posts)`, `Posts` is already in memory — Popcorn will still honor `?include=`
  and omit it from the response, but the DB round-trip has already happened. Use lazy loading
  (or a projection at the query layer) when cost matters.
- **N+1 is still N+1.** If you lazy-load `Posts` inside a loop of 500 authors, that's 500
  queries. Popcorn skipping the fetch when the client doesn't ask for `Posts` is what keeps
  you out of the N+1 zone in the common case — but if a client does ask for it, plan for the
  query count.

## See also

- [Default Includes](DotNetTutorialDefaultIncludes.md) for controlling what a bare request returns.
- [Performance](../Performance.md) for the benchmarked cost of selective inclusion.
