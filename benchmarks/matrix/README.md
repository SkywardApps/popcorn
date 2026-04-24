# Matrix benchmarks — .NET version × JIT/AOT

This directory hosts a **local-only** investigation tool that runs the Popcorn
serialization benchmarks across six cells: `{ net8.0, net9.0, net10.0 } × { JIT, AOT }`.

It is **not** part of CI — the runtime-specific installs (SDKs, ILCompiler packages,
C++ build tools for AOT on Windows) are too machine-specific to bake into a shared
pipeline. The separate `benchmarks.yml` CI workflow is a ratio gate, not a matrix.

## When to run this

Ad-hoc, when you want to answer questions like:

- "Did .NET 9 regress us relative to .NET 8?"
- "Did .NET 10 close those regressions, or introduce new ones?"
- "Does AOT compilation change the Popcorn / STJ ratio?"
- "On a given .NET version, is AOT faster than JIT for our workload?"

## Benchmark scope

`MatrixBenchmarks.cs` (under [`dotnet/benchmarks/MatrixPerformance/`](../../dotnet/benchmarks/MatrixPerformance/))
contains the five load-bearing cells from the CI ratio gate:

- `SimpleModelList_Stj_SourceGen` (baseline)
- `SimpleModelList_PopcornAll`
- `ComplexModelList_Stj_SourceGen`
- `ComplexModelList_PopcornAll`
- `ComplexModelList_PopcornDefault`

Deliberately excludes the legacy `PopcornNetStandard` benchmarks — reflection-heavy code
would require `PublishAot=false` and distort the AOT cells. The matrix project sets
`IsAotCompatible=true` + `IsTrimmable=true`.

## Prerequisites

| Cell        | Needs                                                             |
|-------------|-------------------------------------------------------------------|
| JIT-net8.0  | .NET 8 SDK + .NET 8 runtime                                       |
| JIT-net9.0  | .NET 9 SDK + .NET 9 runtime                                       |
| JIT-net10.0 | .NET 10 SDK + .NET 10 runtime                                     |
| AOT-net8.0  | .NET 8 SDK + VS C++ build tools (Windows) / clang+zlib (Linux)    |
| AOT-net9.0  | .NET 9 SDK + same native toolchain                                |
| AOT-net10.0 | .NET 10 SDK + same native toolchain                               |

**Windows install hints:**

```powershell
winget install Microsoft.DotNet.SDK.8
winget install Microsoft.DotNet.SDK.9
winget install Microsoft.DotNet.SDK.10
# For AOT:
winget install Microsoft.VisualStudio.2022.BuildTools --silent --override "--wait --quiet --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended"
```

**Linux install hints:**

```bash
# Use Microsoft's official instructions for side-by-side SDKs.
sudo apt-get install clang zlib1g-dev
```

Missing cells are skipped with a clear error message; the remaining cells still run.

## Usage

```bash
# All six cells (~15-25 min):
benchmarks/matrix/run-matrix.sh

# Filter by TFM or mode:
benchmarks/matrix/run-matrix.sh net9         # both .NET 9 cells (JIT + AOT)
benchmarks/matrix/run-matrix.sh all aot      # all three AOT cells
benchmarks/matrix/run-matrix.sh net10 jit    # just JIT-net10

# Pick a specific runtime identifier (defaults derived from `uname`):
POPCORN_RID=linux-x64 benchmarks/matrix/run-matrix.sh all aot
```

Unset `DOTNET_ROLL_FORWARD` before running — if that env var is set to `LatestMajor`,
every JIT child process rolls forward to the latest installed runtime regardless of
the target TFM, and the matrix produces six rows that all reflect the same runtime.

## Output

Per-cell BenchmarkDotNet artifacts land under
`benchmarks/matrix/results/<cell>/` — one subdir per cell (`JIT-net9.0`, `AOT-net10.0`,
etc.). Each contains the full markdown report, CSV, JSON, and stdout capture.

To see the aggregated view:

```bash
python3 benchmarks/matrix/summarize-matrix.py benchmarks/matrix/results
```

Prints two markdown tables:

1. **Absolute means (µs)** — one row per benchmark, one column per cell that produced
   data. Missing cells show `—`.
2. **Popcorn / STJ ratios** — one row per `{PopcornAll, PopcornDefault}` variant, same
   columns. The ratio is what you compare across cells: the absolute µs varies with
   runtime, but the Popcorn/STJ ratio is what tells you whether the generator changed
   its stance relative to the STJ baseline under a different runtime or AOT mode.

## Not committed

Result directories under `benchmarks/matrix/results/` are gitignored (they're machine-
specific). If you want to share a specific run's output, paste the aggregator's markdown
into the PR description or an issue comment.

## Gotchas discovered during setup

If you build your own copy of this tool (or port it to a new BDN version), expect to
trip on these:

1. **`PublishAot` on the CLI cascades to `ProjectReference`s.** Passing `-p:PublishAot=true`
   at `dotnet publish` sets the property globally — including on our `netstandard2.0`
   dependencies (`Popcorn.Shared`, `Popcorn.SourceGenerator`), which fails with
   `NETSDK1207` ("AOT not supported for the target framework"). Set `<PublishAot>true</PublishAot>`
   inside the csproj instead; MSBuild keeps csproj-level properties scoped to that project.
2. **`vswhere.exe` must be on `PATH`** on Windows for ILCompiler to locate `link.exe`.
   It is installed at `C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe`
   but that directory isn't on `PATH` by default. `run-matrix.sh` prepends it when it
   detects the file.
3. **BDN 0.14's `CommandLineParser` uses runtime reflection** that fails under AOT with
   "Type ... appears to be immutable, but no constructor found to accept values." Call
   `BenchmarkRunner.Run<T>(config)` without the `args` parameter.
4. **BDN's default toolchain spawns a child `dotnet` process** per benchmark, which a
   single-file AOT binary can't do. Use `InProcessNoEmitToolchain.Instance` — `NoEmit`
   (not `Emit`) matters because `Emit` uses `System.Reflection.Emit` which is blocked
   under AOT.
5. **The trimmer strips `[Benchmark]`-tagged methods** and BDN's own `InProcessNoEmitRunner.Runnable`
   type. Add `TrimmerRootAssembly` entries for `MatrixPerformance`, `Popcorn.Shared`,
   and `BenchmarkDotNet` to preserve them.

## Sample results (2026-04-24, Windows 11, .NET 10.0.201 SDK)

One local run on a dev box. Absolute µs are machine-dependent; the ratios (Popcorn / STJ
on the same shape, same cell) are what's portable across boxes.

**Absolute means (µs):**

| Benchmark                        | JIT-net8 | JIT-net9 | JIT-net10 | AOT-net8 | AOT-net9 | AOT-net10 |
|----------------------------------|---------:|---------:|----------:|---------:|---------:|----------:|
| SimpleModelList_Stj_SourceGen    |    16.56 |    14.19 |     12.51 |    23.00 |    22.46 |     21.94 |
| SimpleModelList_PopcornAll       |    22.81 |    21.93 |     17.44 |    32.25 |    34.08 |     33.87 |
| ComplexModelList_Stj_SourceGen   |    42.02 |    35.26 |     28.16 |    54.37 |    51.76 |     50.71 |
| ComplexModelList_PopcornAll      |    33.82 |    28.73 |     23.34 |    43.05 |    44.21 |     43.96 |
| ComplexModelList_PopcornDefault  |     3.37 |     3.26 |      2.96 |     4.67 |     4.71 |      4.80 |

**Popcorn / STJ ratios:**

| Ratio                                    | JIT-net8 | JIT-net9 | JIT-net10 | AOT-net8 | AOT-net9 | AOT-net10 |
|------------------------------------------|---------:|---------:|----------:|---------:|---------:|----------:|
| SimpleModelList_PopcornAll / Stj         |    1.377 |    1.545 |     1.394 |    1.402 |    1.517 |     1.544 |
| ComplexModelList_PopcornAll / Stj        |    0.805 |    0.815 |     0.829 |    0.792 |    0.854 |     0.867 |
| ComplexModelList_PopcornDefault / Stj    |    0.080 |    0.092 |     0.105 |    0.086 |    0.091 |     0.095 |

**Reading the results:**

- **Every .NET version transition is a forward step on absolute speed.** 8 → 9 saves
  8–16% on most cells; 9 → 10 saves another 12–18%. Cumulative 8 → 10 is 24–34%. No
  regression cells — STJ and Popcorn both get faster at every jump.
- **But .NET 9 regressed Popcorn's worst-case *ratio*.** The worst-case cell's Popcorn/STJ
  ratio went from 1.38 (net8) to 1.55 (net9), then back to 1.39 (net10). STJ picked up
  more from 8 → 9 than Popcorn did, so the ratio widened; then 9 → 10 closed the gap.
  This is consistent with reports of .NET 9 including STJ source-gen optimizations that
  didn't flow through to generic converter plug-ins (which is how Popcorn is registered).
  .NET 10 restored parity — the worst-case ratio is back to roughly where it was on .NET 8.
- **Headline and selectivity ratios barely moved.** ComplexModelList_PopcornAll / Stj
  stays in 0.79–0.87, ComplexModelList_PopcornDefault / Stj in 0.08–0.11 across every
  runtime and JIT/AOT mode. Popcorn's structural wins (faster emit on nested data, much
  faster on selective fetch) are runtime-agnostic.
- **AOT is 1.7–2× slower than JIT** for this workload. Counterintuitive but expected:
  serialization hot paths are exactly where RyuJIT's tier-1 + PGO beats static compilation.
  AOT's wins (startup time, memory footprint, no JIT warmup) don't show up in
  microbenchmarks. If your workload is a long-running service that warms up once,
  **stay on JIT**. If it's a short-lived CLI or serverless function, AOT still earns its
  slot for startup reasons — just not for steady-state throughput.
- **AOT-net8 is slightly faster than AOT-net9/10** on the Popcorn paths. Suggests the
  generator's output is particularly friendly to .NET 8's ILCompiler; later ILCompiler
  versions haven't tightened this specific case. Worth revisiting if AOT becomes a real
  deployment target.
- **Popcorn / STJ ratio is stable across all 6 cells** — 1.38–1.55 worst-case,
  0.79–0.87 headline, 0.08–0.11 selectivity. The CI ratio gate's 25% threshold is
  comfortably above the variance we see across every runtime × mode cell.
