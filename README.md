<img src="/media/PopcornLogo.png" width="50%">

# Popcorn

[![NuGet (v8 preview)](https://img.shields.io/nuget/vpre/Skyward.Api.Popcorn.SourceGen.Shared?label=v8%20preview)](https://www.nuget.org/packages/Skyward.Api.Popcorn.SourceGen.Shared)
[![NuGet (v7)](https://img.shields.io/nuget/v/Skyward.Api.Popcorn?label=v7%20stable)](https://www.nuget.org/packages/Skyward.Api.Popcorn)
[![Tests](https://github.com/SkywardApps/popcorn/actions/workflows/tests.yml/badge.svg?branch=master)](https://github.com/SkywardApps/popcorn/actions/workflows/tests.yml)
[![AOT CI](https://github.com/SkywardApps/popcorn/actions/workflows/aot-ci.yml/badge.svg?branch=master)](https://github.com/SkywardApps/popcorn/actions/workflows/aot-ci.yml)

[Table Of Contents](docs/TableOfContents.md)

> **Quick orientation.** Popcorn v8 (the current active line) is a Roslyn **source generator**.
> The older v7 packages (still on NuGet) were built on **runtime reflection**. The two are
> side-by-side installable and 100% wire-compatible — same `?include=` grammar, same attribute
> semantics. The source-gen rewrite exists so Popcorn can run under `PublishAot=True` and
> `PublishTrimmed=True`, which reflection can't. New projects: start with v8. Existing
> projects on v7: see [Migrating from v7 to v8](docs/MigrationV7toV8.md) — the migration is
> mostly find-and-replace.

## Jump straight in

+ **New in .NET?** [DotNet Quick Start](docs/dotnet/DotNetQuickStart.md) — drop Popcorn v8 into a minimal-API app in five minutes.
+ **Migrating from v7?** [Migration guide](docs/MigrationV7toV8.md) — attribute renames, startup config changes, dropped features.
+ **Curious about numbers?** [Performance](docs/Performance.md) — benchmarked vs raw System.Text.Json and v7 reflection.
+ **Other platforms?** The protocol is platform-agnostic; only .NET has a provider today. See [Roadmap](roadmap.md).

## What is Popcorn?

Popcorn is a communication protocol on top of a RESTful API that allows requesting clients to
identify individual fields of resources to include when retrieving the resource or resource
collection.

It allows for a recursive selection of fields, allowing multiple calls to be condensed
into one.

### Features (v8)

+ Selective field inclusion via `?include=[Field,Nested[Field]]` — one round trip instead of N.
+ Configurable [response defaults](docs/dotnet/DotNetTutorialDefaultIncludes.md) via `[Default]` / `[Always]` / `[Never]` attributes.
+ [Custom response envelopes](docs/MigrationV7toV8.md#6-custom-response-envelope--error-handling) via marker attributes + generator-emitted exception middleware.
+ Works under `PublishAot=True` and `PublishTrimmed=True`. No runtime reflection on the hot path.
+ Beats raw `System.Text.Json` on complex nested data (0.87× time / 0.93× alloc when emitting everything; ~10× faster and ~10× less alloc on selective fetch).

**Ok, so.... what is it in action?**

Okay, maybe some examples will help!

Lets say you have a REST API with an endpoint like so:

``` https://myserver.com/api/1/contacts ```

Which returns a list of contacts in the form:

``` 
[
 {
   "Id":1,
   "Name":"Liz Lemon"
 },
 {
   "Id":2,
   "Name":"Pete Hornberger"
 },
 {
   "Id":3,
   "Name":"Jack Donaghy"
 },
 ...
}
```

Now, if you want to get a list of phone numbers for each of those, you now need to make a series
of calls to further endpoints, one for each contact you want to look up the information for:

``` https://myserver.com/api/1/contacts/1/phonenumbers ```
```
[
  {"Type":"cell","Number":"867-5309"}
]
```
``` https://myserver.com/api/1/contacts/2/phonenumbers ```
```
[
  {"Type":"landline","Number":"555-5555"}
]
```
``` https://myserver.com/api/1/contacts/3/phonenumbers ```
```
[
  {"Type":"cell","Number":"123-4567"}
]
```

That's quite a lot of overhead and work!  Popcorn aims to simplify this at the client's request.
Let's say that while we want the numbers for each contact, we don't really need the type of the number
(cell or landline) and would prefer to save the bandwidth by not transfering it.  Now, instead of 
making many calls, all the above can be reduced down to:

``` https://myserver.com/api/1/contacts?include=[Id,Name,PhoneNumbers[Number]] ```

Which provides:

```
[
 {
    
   "Id":1,
   "Name":"Liz Lemon",
   "PhoneNumbers":
   [
    {
     "Number":"867-5309"
    }
   ]
 },
 {
   "Id":2,
   "Name":"Pete Hornberger",
   "PhoneNumbers":
   [
    {
     "Number":"555-5555"
    }
   ]
 },
 {
   "Id":3,
   "Name": "Jack Donaghy",
   "PhoneNumbers":
   [
    {
     "Number":"123-4567"
    }
   ]
 },
 ...
}
```

Presto! All the information we wanted at our fingertips, and none of the data we didn't!

## Why would I use it?

By implementing the Popcorn protocol, you get a consistent, well-defined API abstraction that your
API consumers can easily utilize. Right now this ships as a C# library for ASP.NET Core with
`System.Text.Json`; the protocol itself is platform-agnostic and other providers are welcome.

### Pros
+ Fewer round trips for nested data.
+ Smaller payloads — server never materializes fields the client isn't going to read.
+ AOT- and trim-safe (v8) — your Popcorn-enabled service can publish to a single native binary.
+ Plain REST + JSON — no new client runtime, visible in the URL, cacheable by HTTP-standard tooling.

### Cons
+ `?include=[...]` is a new grammar clients must learn (though it's easy — see [Include Parameter Syntax](docs/dotnet/DotNetTutorialIncludeParameterSyntax.md)).
+ If you need typed client generation (OpenAPI, etc.), you'll need extra tooling — the shape of a response depends on the request.

## A short history (reflection → source generator)

Popcorn v1 through v7 (on NuGet as `Skyward.Api.Popcorn` / `.DotNetCore`) implemented selective
serialization with **runtime reflection**: at request time the library walked the response
object, read `[IncludeByDefault]` / `[InternalOnly]` attributes, and built a filtered
`Dictionary<string, object?>` before handing it to the JSON serializer. That worked well on
classic ASP.NET Core but became a blocker for newer .NET deployment stories:

- **Native AOT** (`PublishAot=True`) strips metadata the reflection path needs. Popcorn v7 cannot AOT-publish.
- **IL trimming** (`PublishTrimmed=True`) removes reachable-but-reflection-only members.
  Again, v7 loses.
- **Overhead on every request** — reflection + intermediate dictionary allocations dominated
  the hot path, especially for large nested responses.

**Popcorn v8** is a rewrite on top of a Roslyn source generator. At build time the generator
scans `[JsonSerializable(typeof(ApiResponse<T>))]` declarations, walks the type graph, and
emits one straight-line `JsonConverter<T>` per reachable type — no reflection, no intermediate
dictionary, no metadata the trimmer can strip. The wire protocol (`?include=` grammar, attribute
semantics, `Pop<T>` envelope shape) is unchanged; only the internals and the extension-point API
surface moved. See [Performance](docs/Performance.md) for the numbers and
[Migrating from v7 to v8](docs/MigrationV7toV8.md) for the API changes.

Both package lines are on NuGet side-by-side during the transition: grab v8 for new work,
keep v7 if you aren't yet ready to migrate.

## How can I use it in my project?

**.NET (ASP.NET Core):** see the [.NET Quick Start](docs/dotnet/DotNetQuickStart.md) and the
[Getting Started tutorial](docs/dotnet/DotNetTutorialGettingStarted.md). Both packages
(`Skyward.Api.Popcorn.SourceGen` + `Skyward.Api.Popcorn.SourceGen.Shared`) are on NuGet.

**Other platforms:** the protocol is documented in [Documentation](docs/Documentation.md) — if
you build a provider in another language, the `?include=` grammar and default/always/never
semantics defined there are the contract you need to satisfy. Contributions welcome!

## Further Reading

+ [Quick Start](docs/QuickStart.md)
+ [Documentation](docs/Documentation.md)
+ [Performance](docs/Performance.md)
+ [Migrating from v7 to v8](docs/MigrationV7toV8.md)
+ [Roadmap](roadmap.md)
+ [Releases and Release Notes](docs/Releases.md)
+ [Contributing](docs/Contributing.md)
+ [License](LICENSE)
+ [Meet the Maintainers](docs/Maintainers.md)
