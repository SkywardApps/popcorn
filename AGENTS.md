# Agent Instructions: Popcorn

These instructions apply to every coding agent working in this repository (Claude Code, Codex, etc.). `CLAUDE.md` is a thin pointer to this file.

# Your approach (CRITICALLY IMPORTANT)

It is always acceptable to reply that you don't know the answer to something. You may always ask clarifying questions, prompt for more information, or perform additional research to become more confident in a response or task. You may push back and request confirmation if something doesn't make sense, isn't correct, or isn't the right course of action. Be critical, skeptical, and cautious, but ultimately perform the task requested and don't be a roadblock, just make sure it is the best quality it can be.

Use short sentences. No filler, preamble, or pleasantries. Run tools first, show the result, then stop. Do not narrate unless the situation is exceptional.

# Quick facts

- .NET solution at `dotnet/Popcorn.sln`. Requires .NET 8 SDK+.
- Build: `cd dotnet && dotnet build Popcorn.sln`.
- Test: `dotnet test dotnet/Tests/Popcorn.FunctionalTests` and `dotnet test dotnet/Tests/Popcorn.SourceGenerator.Tests`. Expected: 182 passed / 2 skipped, and 19 passed. Treat any new failure or new skip as a regression.
- Generated converter source is inspectable under consuming projects' `obj/Generated/`.
- Shared step-by-step procedures (verify a change, run benchmarks, cut a release, update the memory bank) live in `.claude/skills/*/SKILL.md`. They are plain markdown — any agent can open and follow them; Claude Code can also invoke them as skills.
- Code philosophy: one improvement at a time, fully tested before the next; maintain or improve performance; minimalist code.

# Memory Bank

The Memory Bank (`memory-bank/*.md`) is the **only** persistent project context and is authoritative. Read every file below before starting work. Claude Code inlines them automatically via the `@` imports at the end of this section; other agents must open them manually.

### File map

- **projectbrief.md** — baseline: purpose, requirements, scope. Primary reference for all other files.
- **productContext.md** — problem → solution, functional intent, UX expectations.
- **systemPatterns.md** — architecture, design choices, critical flows, load-bearing generator conventions. Read before touching `ExpanderGenerator.cs`.
- **techContext.md** — stack, solution layout, constraints, CI, tooling.
- **apiDesign.md** — the shipped v8 API surface and per-feature scope ledger.
- **migrationAnalysis.md** — historical decision record for the v7→v8 rewrite.
- **activeContext.md** — current state, guardrails, open work.
- **progress.md** — what works, pitfall ledger, validation status.

## Update Rules

Update the Memory Bank when:

1. New patterns emerge
2. Significant changes occur
3. The user triggers **update memory bank** (requires re-reading and reassessing **every** file)
4. Context becomes ambiguous

Update process: `ReviewAllFiles → RecordCurrentState → ClarifyNextSteps → CaptureInsights`

When planning (plan mode or otherwise): review memory-bank context first, design the strategy, and make no edits until the plan is settled. When executing: do the task, then record significant changes in the memory bank.

## Core Principle

After each memory reset, the agent has zero context. The Memory Bank must be **precise, minimal, unambiguous, and optimized for LLM consumption**. Its accuracy directly determines agent effectiveness.

## Memory Precedence

The memory-bank files take precedence over any agent-private memory (e.g. Claude Code's auto memory in `~/.claude/projects/.../memory/`). If the two conflict, trust and follow memory-bank, and update the private memory to resolve the conflict.

@memory-bank/projectbrief.md
@memory-bank/productContext.md
@memory-bank/systemPatterns.md
@memory-bank/techContext.md
@memory-bank/apiDesign.md
@memory-bank/migrationAnalysis.md
@memory-bank/activeContext.md
@memory-bank/progress.md
