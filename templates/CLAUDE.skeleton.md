# GAME_TITLE — Development Guidelines

<!-- TEMPLATE: copy the sections below into each B44 game repo's root
     CLAUDE.md, replacing GAME placeholders. These sections are studio
     doctrine — keep them textually in sync with this skeleton and propose
     changes here first. Game-specific sections (scene map, content pointers)
     go below them. -->

## Hard Rules

- **No save backwards-compatibility.** Format changes break old saves — that's intentional. Delete any migration/fallback code you find.
- **Never add `using Godot` to anything under `GAME.Core/`.** Zero Godot dependency is a hard invariant.
- **Thin scene controllers.** Scene scripts translate input/output at the Godot boundary; rules, algorithms, and state machines live in `GAME.Core/` behind concrete collaborators.
- This is a **release game**, not a prototype. Architecture and test coverage decisions should reflect that.

## Studio Conventions

- **`*ActionResult` convention:** domain operations return a per-game result record (occurred/denial-reason/payload shape); the Godot layer renders it. The shapes are per-game — never shared.
- **Injected time and randomness:** all wall-clock reads go through `B44.Common.Interfaces.ITimeSource`; all RNG through `IRandomSource`. Never call `DateTime.Now`, `System.Random`, or `GD.Randi` directly in Core. A nullable `IRandomSource?` parameter means "null = deterministic".
- **Structured logging:** log through `B44.Common.Diagnostics.StructuredGameLogger` with the game's own `LogCategory` constants; the Godot sink lives in one `GodotLoggerFactory`.
- **`*Paths.cs` node paths:** scene node paths are declared once in a paths class, never inline strings in controllers.
- **Engine-free value types with one bridge:** Core uses `Vec2`/`Rgba` (from B44.Common) or game types like `GridPosition`; exactly one extension-method bridge file per type converts to Godot types at the boundary.
- **Single-sourced simulation:** AI/solver simulation must run the real rules on a copied state (`CloneForSimulation`), never a parallel reimplementation of the rules.

## Architecture Ratchet

- Treat roughly 350 physical lines as a review warning for production source files. New production files should normally stay at or below 500 lines; files above 650 lines require a clear cohesion-based reason.
- Existing oversized files must not grow unless the same change performs an extraction and leaves the file smaller overall.
- A change adding more than 100 net lines to a production file already above 350 lines must explicitly consider extracting a cohesive owner first.
- Coordinators coordinate. Move full-feature controls, transaction logic, algorithms, and other independently changing responsibilities behind concrete, plainly named collaborators.
- Do not game the limits with cosmetic partial-class splits, one-method services, interface-per-class patterns, generic utility dumping grounds, or needless factories. Optimize for one intelligible reason to change and the least context needed to modify a feature safely.
- Broad feature batches must inspect their largest per-file line deltas before completion. Cleanup work should handle one hotspot or coherent extraction at a time.
- Declarative catalogs, generated code, tests, and genuinely cohesive algorithms may exceed the normal limit when splitting would make the code harder to understand.

## Sunset Guarantee — Core Product Principle

Every B44 game is **sunset-safe by design**. If active development,
monetization, or any online services end, the final supported release must
remain a complete, playable offline game:

- No developer-operated server, account, advertisement, or further purchase
  may be required to start, progress, unlock gameplay content, save, reload,
  or finish the game.
- Former ad rewards, premium-currency grants, remote events, and other online
  dependencies must receive deterministic local replacements. Preserve the
  progression loop rather than simply marking everything complete.
- All required gameplay content must ship with the final client. Third-party
  SDK or network failure must degrade optional services only, never block play.
- Player progress must remain locally durable and support export/import before
  any online or platform-specific persistence is introduced.
- Maintain an explicit, testable `SunsetMode` once monetization or online
  features exist. Its acceptance test runs without network access and proves a
  fresh player can progress through, unlock, save, reload, and complete the
  game without ads or purchases.

This guarantees independence from services under our control at sunset; it
does not promise compatibility with every future operating system or device.

## Shared Primitives

Engine-free shared code lives in the private `B44.Common` NuGet package
(repo: `DA-Sandman-Jr/B44.Common`, usually cloned as a sibling directory —
read its sources there). Do not copy its types back into the game. Extract a
new primitive TO the package only on its second cross-game occurrence.

## Environment

- `AGENTS.md` files are auto-generated from sibling `CLAUDE.md` on build via `Directory.Build.targets`. Edit the `CLAUDE.md`, not the `AGENTS.md`.
- `Nullable enable` is set globally in `Directory.Build.props` — don't re-declare per-project.
- xunit.v3 gotcha: on .NET SDK 8.0.1xx, `dotnet test` restores and exits without discovering xunit.v3 executables. Test projects need `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`, or run the test project executable directly (`scripts/test.sh`).

## Tests

```bash
dotnet test
```
