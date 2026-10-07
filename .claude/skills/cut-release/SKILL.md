---
name: cut-release
description: Cut a Popcorn release to NuGet. Use when the user asks to release, tag, publish, or ship a version.
---

# Cutting a release

Releases are tag-driven; `.github/workflows/main.yml` does the packing and publishing. Versioning is SemVer. Current line: v8 previews (`8.0.0-preview.N`).

## Procedure

1. Confirm `master` is green (tests, aot-ci, benchmarks workflows).
2. Add an entry to `docs/Releases.md` for the new version (features, breaking changes, date) and commit it via PR first.
3. Tag and push — **the tag must be on master and follow `release/vX.Y.Z`**:
   ```bash
   git tag release/v8.0.0-preview.2
   git push origin release/v8.0.0-preview.2
   ```
4. The workflow then: parses the SemVer from the tag → `dotnet pack` both packages with `-p:Version` → pushes `Skyward.Api.Popcorn.SourceGen` + `Skyward.Api.Popcorn.SourceGen.Shared` (with snupkg) to NuGet with `--skip-duplicate` → creates a GitHub release with `--generate-notes` → best-effort syncs csproj `<Version>` back to master (continues on failure).
5. Verify: check the GitHub Actions run, then the package pages on nuget.org.

## Notes

- Tagging is an outward-facing, hard-to-reverse action — get explicit user confirmation before pushing a tag.
- Legacy v7 (`Skyward.Api.Popcorn`) is NOT packed by CI anymore; v8-only publishing.
- After release, update `memory-bank/activeContext.md` and `roadmap.md` with the new version state.
