# [Popcorn](../README.md) > Contributing

[Table Of Contents](TableOfContents.md)

When contributing to this repository, please first discuss the change you wish to make via issue,
email, or any other method with the owners of this repository before making a change.

Please note we have a [code of conduct](../CODE_OF_CONDUCT.md), please follow it in all your interactions with the project.

## How to contribute

Feel free to check out our issues; we'll track bugs and flaws there, which are usually an easy and quick way to
get into the project and start contributing. If you're feeling bold, take a look at our [Roadmap](../roadmap.md).
Please start a discussion before tackling anything more than trivial tasks to ensure your pull request will be accepted.

There are two levels to this project: the abstract [Specification](Documentation.md) of the protocol and concrete
implementations (e.g. [DotNet](dotnet/DotNetDocumentation.md)). All implementations must adhere to the contract of the
Specification at a minimum.

If you don't want to get into coding just yet, documentation is always appreciated! Get cracking on tutorials, FAQs,
PR walkthroughs, especially if you just went through the process yourself and wish someone had written it up for you.

## Building and testing

Everything lives under `dotnet/` ([Popcorn.sln](../dotnet/Popcorn.sln)); you need the .NET 8 SDK or later.

```bash
cd dotnet
dotnet build Popcorn.sln
dotnet test Tests/Popcorn.FunctionalTests          # main v8 suite (exercises real generated converters)
dotnet test Tests/Popcorn.SourceGenerator.Tests    # generator diagnostics (JSG003–JSG008)
```

Tips:
- Generated converter source is inspectable under each consuming project's `obj/Generated/` tree — invaluable when diagnosing generator issues.
- The AOT example: `dotnet publish PopcornAotExample -c Release` (or build its Dockerfile) to check trim/AOT compatibility.
- Benchmarks: `cd dotnet/benchmarks/SerializationPerformance && dotnet run -c Release -- ci` runs the fast subset used by the CI perf gate. See [Performance](Performance.md).

## CI checks on every PR

- `tests.yml` — both test projects must pass.
- `aot-ci.yml` — builds the AOT example container and asserts its endpoints end-to-end.
- `benchmarks.yml` — compares Popcorn/STJ performance ratios against `benchmarks/results/ci-baseline.json`; fails on >25% regression. If your change legitimately shifts performance, update the baseline in the same PR (run the `ci` benchmark subset locally) and explain why.

## Pull Request Process

1. Keep changes focused: one improvement at a time, fully tested.
2. Update related documentation (`docs/`, `roadmap.md`) with details of behavioral changes.
3. Ensure all three CI checks pass.
4. You may merge once you have the sign-off of one or more maintainers, or if you
   do not have permission to do that, you may request the maintainer merge it for you.

## Releases (maintainers)

Versioning is [SemVer](http://semver.org/). To cut a release, push a tag of the form `release/vX.Y.Z`
(e.g. `release/v8.0.0-preview.1`). The [`main.yml`](../.github/workflows/main.yml) workflow then packs both v8 packages
with that version, pushes them to NuGet, creates a GitHub release with generated notes, and best-effort syncs the
csproj `<Version>` back to master. Add a matching entry to [Releases.md](Releases.md) in the same change.
