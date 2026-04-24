#!/usr/bin/env python3
"""
Aggregate per-cell BenchmarkDotNet JSON reports into a single matrix table.

Reads: benchmarks/matrix/results/<cell>/*-report-full.json
Writes to stdout: one markdown table where rows are benchmarks and columns are
the cells that produced a result. Missing cells (compile failed, runtime not
installed, etc.) show '—'.

Also computes the Popcorn / Stj_SourceGen ratio for each cell and prints a
second table of just the ratios, which is what usually answers "did .NET 9
regress us / did .NET 10 help / did AOT help?".
"""
from __future__ import annotations
import json
import sys
from pathlib import Path

# Ordering: JIT cells first, then AOT. Within each, net8 → net9 → net10.
CELL_ORDER = [
    "JIT-net8.0", "JIT-net9.0", "JIT-net10.0",
    "AOT-net8.0", "AOT-net9.0", "AOT-net10.0",
]


def load_cell(cell_dir: Path) -> dict[str, float]:
    """Find the BDN *-report-full.json in `cell_dir` and return { method: mean_ns }."""
    matches = list(cell_dir.glob("*-report-full.json"))
    if not matches:
        return {}
    with matches[0].open() as f:
        data = json.load(f)
    return {b["Method"]: b["Statistics"]["Mean"] for b in data["Benchmarks"]}


def fmt_us(ns: float | None) -> str:
    if ns is None:
        return "—"
    return f"{ns / 1000:.2f} us"


def fmt_ratio(r: float | None) -> str:
    if r is None:
        return "—"
    return f"{r:.3f}"


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: summarize-matrix.py <results-dir>", file=sys.stderr)
        return 2

    results_dir = Path(sys.argv[1])
    if not results_dir.is_dir():
        print(f"ERROR: {results_dir} is not a directory", file=sys.stderr)
        return 2

    # Load every cell that produced a JSON report.
    cells: dict[str, dict[str, float]] = {}
    for sub in sorted(results_dir.iterdir()):
        if not sub.is_dir():
            continue
        means = load_cell(sub)
        if means:
            cells[sub.name] = means

    if not cells:
        print("No cells produced BDN JSON results.", file=sys.stderr)
        return 1

    # Stable cell order: preferred ordering first, then any stragglers.
    ordered_cells = [c for c in CELL_ORDER if c in cells]
    ordered_cells += [c for c in sorted(cells) if c not in ordered_cells]

    # Union of all benchmark methods seen across cells (stable order).
    all_methods: list[str] = []
    seen: set[str] = set()
    for cell in ordered_cells:
        for m in cells[cell]:
            if m not in seen:
                all_methods.append(m)
                seen.add(m)

    # --- Table 1: absolute means (µs) per (benchmark, cell) ---
    print("## Matrix — absolute means (µs)\n")
    header = "| Benchmark | " + " | ".join(ordered_cells) + " |"
    sep = "|" + "---|" * (len(ordered_cells) + 1)
    print(header)
    print(sep)
    for method in all_methods:
        row = [method]
        for cell in ordered_cells:
            row.append(fmt_us(cells[cell].get(method)))
        print("| " + " | ".join(row) + " |")

    # --- Table 2: Popcorn / Stj_SourceGen ratio per (benchmark, cell) ---
    # Pair each PopcornAll / PopcornDefault benchmark with the Stj_SourceGen benchmark
    # on the same shape. Shape prefix = everything before the last underscore segment.
    print("\n## Matrix — Popcorn / Stj ratios\n")
    print("Ratio = Popcorn{All,Default} mean / Stj_SourceGen mean, same shape, same cell.\n")

    # Build pairs: shape → { 'stj': benchmark_name, 'variants': [list of popcorn benchmark names] }
    pairs: dict[str, dict] = {}
    for m in all_methods:
        if m.endswith("_Stj_SourceGen"):
            shape = m[:-len("_Stj_SourceGen")]
            pairs.setdefault(shape, {})["stj"] = m
        elif m.endswith("_PopcornAll") or m.endswith("_PopcornDefault"):
            # shape = everything before "_Popcorn"
            idx = m.rfind("_Popcorn")
            shape = m[:idx]
            pairs.setdefault(shape, {}).setdefault("variants", []).append(m)

    ratio_header = "| Ratio | " + " | ".join(ordered_cells) + " |"
    print(ratio_header)
    print(sep)
    for shape, pair in pairs.items():
        stj_name = pair.get("stj")
        variants = pair.get("variants", [])
        if not stj_name:
            continue
        for variant in variants:
            label = f"{variant} / {stj_name}"
            row = [label]
            for cell in ordered_cells:
                stj = cells[cell].get(stj_name)
                pop = cells[cell].get(variant)
                if stj and pop and stj > 0:
                    row.append(fmt_ratio(pop / stj))
                else:
                    row.append("—")
            print("| " + " | ".join(row) + " |")

    print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
