---
name: benchmark
description: Run Popcorn serialization benchmarks and manage the CI performance-ratio baseline. Use when a change touches emitted serialization code, when the benchmarks.yml gate fails, or when the user asks for performance numbers.
---

# Benchmarks and the CI perf gate

## Quick gate check (what CI runs, ~2 min)

```bash
cd dotnet/benchmarks/SerializationPerformance
dotnet run -c Release -- ci
```

Runs 5 filtered benchmarks (SimpleJob, 3 warmup + 15 iterations). Then compare ratios (from repo root):

```bash
python .github/scripts/compare-benchmark-ratios.py \
  dotnet/benchmarks/SerializationPerformance/BenchmarkDotNet.Artifacts/results/SerializationPerformance.Benchmarks.SerializationComparisonBenchmarks-report-full.json \
  benchmarks/results/ci-baseline.json
```

The gate compares three **Popcorn/STJ ratios** against `benchmarks/results/ci-baseline.json` and fails on >25% regression. Ratios, not absolute times: runner wall-clock varies ±20–30%, ratios within one run are stable (±5%).

Baseline ratios (for orientation): `SimpleModelList_PopcornAll_vs_Stj` ≈ 1.49 (worst case), `ComplexModelList_PopcornAll_vs_Stj` ≈ 0.87 (faster than STJ), `ComplexModelList_PopcornDefault_vs_Stj` ≈ 0.11.

## Baseline update discipline

- Real optimization shipped → re-run `ci` locally, commit the new `benchmarks/results/ci-baseline.json` **in the same PR**.
- Intentional perf trade (feature > speed) → bump the baseline upward and say why in the PR.
- Never update the baseline just to make CI pass on an unexplained regression.

## Full suites (slow, DefaultJob)

```bash
cd dotnet/benchmarks/SerializationPerformance
dotnet run -c Release -- comparison   # 3-way: STJ vs Popcorn v8 vs legacy v7
# other selectors: includes | scalability | circular | attributes | all
```

Include-parser micro-benchmarks: `cd dotnet/benchmarks/ParsingIncludes && dotnet run -c Release`.

Committed reference results + methodology: `benchmarks/results/v2-baseline/` ("v2" = pre-release name of v8). User-facing summary: `docs/Performance.md`.
