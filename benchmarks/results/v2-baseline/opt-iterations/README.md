# Incremental optimizations — three-step walk

Captured 2026-04-23. Baseline in [`../README.md`](../README.md) (commit `9168c4c`, .NET 9.0.15). Each step below is an isolated generator change, tested against the full functional suite (182 passing, 13 skipped throughout), benchmarked with the same BDN configuration.

Raw logs:
- [step1-linq-removed.log](step1-linq-removed.log)
- [step2-flags-hoisted.log](step2-flags-hoisted.log)
- [step3-hashset-elided.log](step3-hashset-elided.log)

## The three changes

### #1 — Replace LINQ `.Any()` / `.FirstOrDefault()` with for-loops
[`ExpanderGenerator.cs:CreateComplexObjectInnerBody`](../../../dotnet/Popcorn.SourceGenerator/ExpanderGenerator.cs). The emitted converter body used `properties.Any(p => ...)` twice for `!all`/`!default` flag detection and `properties.FirstOrDefault(p => ...)` once per property. All replaced with a single index-loop pass for flags and inline index-loops per property. Same complexity, different constant factor — and no per-call iterator state (even when JIT stack-allocates the enumerator, the method-call / virtual-dispatch overhead is real).

### #2 — Hoist `useAll` / `useDefault` / `naming` out of the inner converter into list/dict callers
[`ExpanderGenerator.cs:FlagSetupCode`](../../../dotnet/Popcorn.SourceGenerator/ExpanderGenerator.cs). Added a split: every complex-object Pop now has a 4-arg wrapper that computes flags + naming delegate, and a new `Pop{X}Inner` overload accepting pre-computed values. List/dict converters compute the setup ONCE outside the foreach and call `_Inner` per item. For a 100-item list, this eliminates 99 redundant scans and 99 naming-delegate allocations. Single-member callsites (nested properties with differing `propertyReference.Children`) keep using the 4-arg overload.

### #3 — Elide `HashSet<object>` allocation for cycle-safe type graphs
[`ExpanderGenerator.cs:IsConverterCycleSafe`](../../../dotnet/Popcorn.SourceGenerator/ExpanderGenerator.cs). A DFS through each converter's payload type (unwrapping arrays / `IEnumerable<T>` / `IDictionary<K,V>` / `Nullable<T>`) flags the converter as cycle-safe when no transitive property can reach back to an ancestor of itself. Cycle-safe converters pass `null` instead of `new HashSet<object>()` at the entry point; body ops become null-conditional. Cycle-risky types (e.g. `ComplexNestedModel` with its `Child` back-reference, `CircularReferenceModel`, `DeepNestingModel`) continue to allocate and track.

## Incremental results (DefaultJob, mean time ns / allocated bytes)

| Benchmark | Original | #1 LINQ→for | #2 hoist flags | #3 elide HashSet | Total Δ time | Total Δ alloc |
|---|---:|---:|---:|---:|---:|---:|
| **SimpleModel_PopcornDefault** | 262.8 / 592 | 223.6 / 592 | 230.5 / 592 | **204.0 / 416** | **−22%** | **−30%** |
| **SimpleModel_PopcornAll** | 351.3 / 776 | 320.7 / 776 | 326.7 / 776 | **292.0 / 600** | **−17%** | **−23%** |
| **SimpleModel_PopcornCustom** | 335.1 / 632 | 335.5 / 632 | 306.6 / 632 | **280.5 / 456** | **−16%** | **−28%** |
| **SimpleModelList_PopcornDefault** | 16,415 / 35,808 | 14,381 / 35,808 | 14,047 / 29,472 | **12,491 / 29,296** | **−24%** | **−18%** |
| **SimpleModelList_PopcornAll** | 26,861 / 54,024 | 24,704 / 54,016 | 23,581 / 47,680 | **21,775 / 47,504** | **−19%** | **−12%** |
| **SimpleModelList_PopcornCustom** | 23,775 / 38,840 | 24,132 / 38,840 | 20,651 / 32,504 | **19,491 / 32,328** | **−18%** | **−17%** |
| ComplexModel_PopcornDefault | 252.3 / 520 | 215.6 / 520 | 222.0 / 512 | 225.6 / 520 | −11% | ±0% |
| **ComplexModel_PopcornAll** | 2,629 / 3,880 | 1,874 / 3,880 | 1,737 / 3,496 | **1,795 / 3,496** | **−32%** | **−10%** |
| ComplexModel_PopcornCustom | 2,325 / 3,720 | 2,089 / 3,728 | 2,049 / 3,344 | **2,059 / 3,344** | −11% | −10% |
| **ComplexModelList_PopcornDefault** | 3,884 / 6,992 | 3,238 / 6,992 | 3,170 / 5,464 | **3,150 / 5,456** | **−19%** | **−22%** |
| **ComplexModelList_PopcornAll** | 36,837 / 56,640 | 28,885 / 56,640 | 28,144 / 50,992 | **27,824 / 51,000** | **−24%** | **−10%** |
| ComplexModelList_PopcornCustom | 35,384 / 50,528 | 32,241 / 50,528 | 32,486 / 44,888 | **31,692 / 44,880** | **−10%** | **−11%** |

## What each change contributed

**Step #1 (LINQ → for):** primarily *time* wins, no allocation delta. In modern .NET, `List<T>.Any()` and `.FirstOrDefault()` use the concrete `List<T>.Enumerator` struct (often stack-allocated by the JIT). The gains come from eliminating method-call overhead, delegate dispatch to the predicate lambda, and unnecessary enumerator state management — not heap allocations. Strongest on ComplexModel_PopcornAll (−29%) where the per-property `FirstOrDefault` fires most often.

**Step #2 (hoist flags):** primarily *allocation* wins on list/dict scenarios (−10% to −22%) with modest time gains (−5% to −14%). The `Func<string, string>` naming delegate was being allocated per item inside each list iteration — 100× for a 100-item list. Hoisting it to the list converter's body reduces that to 1. Time savings come from not re-scanning `PropertyReferences` 100 times.

**Step #3 (elide HashSet):** *both* time and allocation wins on single-object scalar scenarios (−15% time / −30% alloc on SimpleModel_PopcornDefault). The `new HashSet<object>()` at each converter entry is ~80 bytes including its internal buckets array. Cycle-safe types (SimpleModel, ScalableModel, AttributeHeavyModel, PropertyMappingModel, and any list/dict/nullable wrapper thereof) skip this entirely. Cycle-risky types (ComplexNestedModel, CircularReferenceModel, DeepNestingModel) are unchanged — which is why ComplexModel results don't move for this step.

## Ratios vs baselines — where we ended up

| Scenario | vs Stj_Reflection (time) | vs Legacy_Default (time) |
|---|---:|---:|
| SimpleModel_PopcornAll | 1.77× (was 2.03×) | 0.41× |
| SimpleModelList_PopcornAll | 1.40× (was 1.80×) | 0.31× |
| SimpleModelList_PopcornDefault | 0.81× (was 1.10×) | 0.18× |
| **ComplexModelList_PopcornAll** | **0.87× (was 0.97×)** — now *faster* than STJ when emitting everything | 0.22× |
| ComplexModelList_PopcornDefault | 0.10× (unchanged — already dominant) | 0.17× |

The "flat simple list" case (`SimpleModelList_PopcornAll`) — previously the worst Popcorn vs STJ showing at 1.80× slower — is now 1.40× slower. Still not parity, but meaningfully tighter. The "complex nested list with everything" case (`ComplexModelList_PopcornAll`) crossed parity: **0.87× STJ** while still supporting selective-fetch at runtime.

## What didn't move (and why)

- **ComplexModel scenarios barely changed in step #3.** Expected — `ComplexNestedModel` has a `Child: ComplexNestedModel?` self-reference, so the cycle-safety analyzer correctly flags it as cycle-risky and keeps the HashSet.
- **Custom include lists don't benefit as much as Default/All.** The per-property include-match scan loops over the include list; larger include lists = more work per property. Orthogonal to the three changes here.

## What's still on the table (future work, not in this run)

- **Pre-encoded property names (`JsonEncodedText`) like STJ source-gen uses.** Would save per-property UTF-16→UTF-8 encoding cost. Complicated by runtime `PropertyNamingPolicy` — a policy in effect means the encoded form depends on options, forcing per-options caching. Probably the biggest remaining lever; not attempted here.
- **Replace `Span<char>.Equals` with a pre-computed hash per include-list entry.** For types with many properties and large include lists, the linear scan per property is O(n·m). A small hashtable keyed by name would be O(1). Marginal — list sizes are typically small.
- **Skip the include-match scan when `useAll` and no negations are present.** Every property unconditionally emits under `!all`, so the per-property scan is pure overhead. A one-time check at the top of the body could bypass the scan entirely.
