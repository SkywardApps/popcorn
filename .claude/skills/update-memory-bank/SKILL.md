---
name: update-memory-bank
description: Full review-and-refresh of the memory-bank files. Use when the user says "update memory bank", after significant changes land, or when memory-bank content contradicts the repo.
---

# Update the Memory Bank

The memory bank (`memory-bank/*.md`) is the only persistent project context and must stay precise, minimal, unambiguous, and LLM-optimized. Process: `ReviewAllFiles → RecordCurrentState → ClarifyNextSteps → CaptureInsights`.

## Procedure

1. **Establish ground truth first** — don't trust the memory bank's own claims:
   - `git log --oneline -15`, current branch, latest `release/v*` tag.
   - Test counts: run both suites or read the latest CI run.
   - Open roadmap items vs `roadmap.md`.
2. **Re-read every file** in `memory-bank/` (all 8), plus `roadmap.md`. Check each factual claim against ground truth.
3. **Update per file**:
   - `activeContext.md` — current state, guardrails, open work, recent milestones. Most volatile; update first.
   - `progress.md` — what works, test counts, pitfall ledger, validation status.
   - `projectbrief.md` / `productContext.md` — only if scope or positioning changed.
   - `systemPatterns.md` / `techContext.md` — only if architecture, conventions, layout, or CI changed.
   - `apiDesign.md` — only if the public API surface or feature ledger changed.
   - `migrationAnalysis.md` — historical record; rarely changes.
4. **Dedupe**: each fact lives in exactly one file; others link to it. Canonical homes: feature ledger → `apiDesign.md`; generator conventions → `systemPatterns.md`; test/perf status → `progress.md`; open work → `roadmap.md` (activeContext points there).
5. **Style**: short sentences, concrete file paths and commit hashes, absolute dates (never "recently" or "current branch" without naming it). Write for a lower-capability model with zero context.
6. Keep `AGENTS.md` quick-facts (test counts, commands) in sync if they changed.
