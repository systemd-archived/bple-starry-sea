# AGENT.md

This file provides context for AI coding agents (Claude Code and other agentic tools) working in this repository.

## Project Overview

This repository, **BPLE 2022.1.9**, is a decompilation project of a Unity game. "BPLE" refers to a beta/technology version line, and `2022.1.9` denotes this specific beta build being decompiled and restored to a working state.

Current status of this repo:
- The project is a **raw decompilation** of the original Unity game build.
- Shader recreation has been properly set up and verified — shaders work correctly.
- The Unity project **builds successfully on Windows**.
- AssetBundles are **not yet generated for other platforms** (e.g. Android, Linux, macOS, etc.) — this still needs work.

In short: this repo is functional decompiled source + fixed shaders, but it has not received the deeper quality-of-life and correctness passes that a sister project has.

## Related Project: BPLE 2022.1 (Anstro Pleuton)

There is a related, more mature project: **BPLE 2022.1** (note: no `.9` — a different beta build in the same BPLE line).

- Repo: https://github.com/anstropleuton/BPLE
- Maintainer Anstro Pleuton has spent significant time on this project making quality-of-life and correctness improvements beyond the raw decompilation baseline that this repo (`2022.1.9`) currently has.
- **Claude should treat this repo as a reference/upstream source of fixes**, not as a dependency to pull in wholesale — `2022.1.9` is a different build number, so changes need to be evaluated/ported deliberately rather than copy-pasted blindly.

### Why this matters

Because BPLE 2022.1.9 is currently "just decompilation + fixed shaders," a meaningful chunk of future work here is **porting relevant improvements from the Anstro Pleuton repo** into this one, adapted as needed for the `2022.1.9` build differences.

Known concrete gap:
- **Multi-platform AssetBundle generation is missing/broken in this repo.** The Anstro Pleuton repo has working solutions for this that involve (at least) `Bundle.cs` and `Orchestrate.cs`. These need to be reviewed and ported over here, adapting for any differences between `2022.1` and `2022.1.9`.

Other changes from the Anstro Pleuton repo may also be worth porting over on a case-by-case basis (bug fixes, editor tooling, build pipeline improvements, etc.) — not everything is mandatory, but it's a good first place to look when something is broken or missing in this repo.

## Guidance for the Agent

- When working on a problem in this repo — especially anything related to AssetBundles, the build pipeline, editor tooling, or anything that "feels like it should already work" — **check the BPLE 2022.1 repo (https://github.com/anstropleuton/BPLE) first** to see whether it's already been solved there.
- When porting code from that repo, keep in mind the build/version differs (`2022.1` vs `2022.1.9`), so verify compatibility rather than assuming a drop-in copy will work.
- Prioritize the AssetBundle/multi-platform build gap (`Bundle.cs`, `Orchestrate.cs`, and related build scripts) as a known, named priority when relevant work comes up.
- Do not assume parity with the Anstro Pleuton repo — this repo is intentionally behind it in quality/completeness. When in doubt about "is this the right way to do X in this codebase," compare against how it's done there.
