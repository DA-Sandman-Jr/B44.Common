# GameName — Development Guidelines

Repository-local guidance. The managed B44 organization and game sections are
inserted and kept current by `B44.Standards` on every build — do not hand-edit
them, and do not edit `AGENTS.md` at all; it is generated from this file.

## What this game is

_One paragraph: the pitch, the platform, the state of it. Replace this._

## Layout

- `GameName.Core/` — engine-free. All rules, state, algorithms, and anything
  worth testing. Sets `B44EngineFreeCore=true`; a Godot reference here fails
  the build.
- `GameName.Tests/` — xunit.v3 over Core only. Carries a guard target that fails
  if a Godot dependency reaches the test graph.
- Godot project at the repository root once it exists; scene controllers stay
  thin and translate at the boundary.

## Ratchet

`ratchet-baseline.txt` records every production file over 350 lines. A violation
fails **the build**, not just the suite. Regenerate only in the same change that
performs a real extraction:

```bash
dotnet build GameName.Core/GameName.Core.csproj -t:B44WriteRatchetBaseline
```

An entry's trailing `# reason` comment survives regeneration. When the build
fails on the ratchet the fix is the extraction — do not split a file in a way
that leaves the code worse, and do not raise a baseline entry to fit. If a file
genuinely warrants an exception, stop and ask David; **an agent must never grant
itself one.**

## Tests

```bash
dotnet test
```
