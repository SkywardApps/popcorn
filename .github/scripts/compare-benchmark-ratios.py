#!/usr/bin/env python3
"""
Compare BenchmarkDotNet output against a committed ratio baseline.

Reads:
  - BenchmarkDotNet JSON results (path passed as arg 1).
  - A baseline file (path passed as arg 2) — `benchmarks/results/ci-baseline.json`.

For each entry in the baseline's `ratios` array, computes
    ratio = means[numerator] / means[denominator]
and compares against `value`. Exits nonzero if any ratio regressed by more than
`thresholdPercent` (current / baseline - 1 > thresholdPercent / 100). A ratio getting
BETTER than the threshold prints an informational note but does not fail.

Rationale: absolute ns/op is runner-dependent (+/- 20-30% between GH runners), but
within a single run both numerator and denominator see the same noise, so their ratio
stays stable. Detecting regressions in the RATIO is what we care about.
"""
from __future__ import annotations
import json
import sys
from pathlib import Path


def load_means(bdn_json_path: Path) -> dict[str, float]:
    """Parse BenchmarkDotNet '*-report-full.json' into { method_name: mean_ns }."""
    with bdn_json_path.open() as f:
        data = json.load(f)
    return {b["Method"]: b["Statistics"]["Mean"] for b in data["Benchmarks"]}


def main() -> int:
    if len(sys.argv) != 3:
        print("usage: compare-benchmark-ratios.py <bdn_results.json> <baseline.json>", file=sys.stderr)
        return 2

    results_path = Path(sys.argv[1])
    baseline_path = Path(sys.argv[2])

    if not results_path.exists():
        print(f"ERROR: results file not found: {results_path}", file=sys.stderr)
        return 2
    if not baseline_path.exists():
        print(f"ERROR: baseline file not found: {baseline_path}", file=sys.stderr)
        return 2

    means = load_means(results_path)
    with baseline_path.open() as f:
        baseline = json.load(f)

    threshold_pct = float(baseline["thresholdPercent"])
    ratios = baseline["ratios"]

    print(f"Baseline threshold: {threshold_pct:.0f}% regression tolerance\n")
    print(f"{'Ratio':<45} {'baseline':>10} {'current':>10} {'delta':>10}  status")
    print("-" * 90)

    failed: list[str] = []
    improved: list[str] = []

    for entry in ratios:
        name = entry["name"]
        baseline_value = float(entry["value"])
        numerator_name = entry["numerator"]
        denominator_name = entry["denominator"]

        numerator = means.get(numerator_name)
        denominator = means.get(denominator_name)
        if numerator is None:
            print(f"FAIL  {name}: numerator '{numerator_name}' not in results")
            failed.append(name)
            continue
        if denominator is None:
            print(f"FAIL  {name}: denominator '{denominator_name}' not in results")
            failed.append(name)
            continue

        current = numerator / denominator
        delta_pct = (current / baseline_value - 1.0) * 100.0
        # Regression: ratio got bigger (slower numerator relative to denominator).
        # Improvement: ratio got smaller. Sign convention holds even for sub-1.0 ratios.
        status = "PASS"
        if delta_pct > threshold_pct:
            status = "FAIL (regression)"
            failed.append(name)
        elif delta_pct < -threshold_pct:
            status = "NOTE (improvement)"
            improved.append(name)

        print(f"{name:<45} {baseline_value:>10.3f} {current:>10.3f} {delta_pct:>+9.1f}%  {status}")

    print()
    if improved:
        print(f"Informational: {len(improved)} ratio(s) improved by more than {threshold_pct:.0f}% "
              f"vs. baseline. If this is from an intentional optimization, update "
              f"benchmarks/results/ci-baseline.json in the same PR so the gate catches future "
              f"regressions from the new, tighter level.")
        for k in improved:
            print(f"  - {k}")
        print()

    if failed:
        print(f"FAIL: {len(failed)} ratio(s) regressed by more than {threshold_pct:.0f}%.")
        for k in failed:
            print(f"  - {k}")
        return 1

    print("PASS: all ratios within tolerance.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
