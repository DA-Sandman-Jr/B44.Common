# B44.Common — Shared Engine-Free Primitives

Private NuGet package (`B44.Common` on GitHub Packages) consumed by the three
B44 game repos: GameA, GameB, GameC. Also the
canonical home for B44-wide standards under `/templates`.

## Hard Rules

- **Engine-free forever.** No `using Godot`, no Godot/GodotSharp package or
  assembly references anywhere in this repo. The test csproj enforces this
  with an MSBuild guard.
- **No game content.** Log categories, content catalogs, tuning values, save
  DTOs, and `*ActionResult` shapes stay in the games. This package ships
  mechanisms, not content.
- **Second-occurrence rule.** A primitive enters this package only when at
  least two games need it (or demonstrably will within the current effort).
  This is not a utility dumping ground.
- **No save backwards-compatibility is a PRE-RELEASE rule.** While a game is
  unreleased, unreadable saves throw `StoreException` and get reset by
  `RepositoryFactory` (after `AtomicJsonFileStore`'s automatic last-good
  `.bak` recovery), never format-migrated. At each game's 1.0 this flips:
  released saves are a compatibility surface, and that game adds a versioned
  envelope + migration chain on top. The store itself stays format-agnostic
  either way.
- **Determinism is API.** `SystemRandomSource` seeded sequences must match
  raw `System.Random` (tests pin this). Changing them breaks game test suites
  downstream.

## Persistence — Decision Record

`AtomicJsonFileStore` stays custom JSON-on-disk (reviewed against
LiteDB/SQLite/Akavache, 2026-07-16). One small human-readable document per
concern beats an embedded database here: no queries or partial updates exist;
JSON + System.Text.Json's tolerant deserialization makes additive save
evolution free and shape-breaking migrations a readable `JsonNode` transform;
and a third-party container adds a SECOND compatibility surface (LiteDB has
broken its own file format between majors) plus native-binary export friction
(SQLite). Durability concerns are answered in-store instead: flush-to-disk
before the rename, and `.bak` rotation with automatic recovery on load — the
tests pin all of it. Versioned-envelope/migration helpers land in this package
only when a second game needs them at 1.0 (GameA' `SaveFileEnvelope` is the
first occurrence).

## Custom Logger — Decision Record & Flip Conditions

`StructuredGameLogger` stays custom (reviewed against MEL/Serilog/ZLogger,
2026-07-16). Rationale: logging frameworks decouple many producers from many
sinks across library boundaries; B44 games have one producer (their own
code), one sink (Godot — which already persists `GD.Print` output to
`user://logs/godot.log`), and zero log-emitting dependencies. The genuine
"wheel" here is ~60 lines, tested once in this package.

Revisit and swap to a standard framework if ANY of these appears:

1. A dependency that accepts/expects `Microsoft.Extensions.Logging.ILogger`
   → adopt MEL abstractions with a custom Godot provider.
2. A real second sink (crash reporting, telemetry, non-Godot file format)
   → Serilog with a custom `ILogEventSink`. (Sunset Guarantee weighs against
   remote telemetry — don't add a sink to justify the swap.)
3. Any component running outside the Godot engine (server, CLI tool)
   → MEL, since it loses the free Godot file sink.

Migration cost is deliberately contained: all call sites go through this one
type, so a swap is a package change + mechanical call-site updates.

## Godot-Ecosystem Survey (2026-07-16)

The Godot-C# ecosystem (Chickensoft et al., verified active as of 2026-04)
was surveyed alongside the mainstream libraries. Structural rule: anything
Godot-specific is definitionally unable to replace code behind the
engine-free wall — it can only compete with the thin Godot-side adapters.

- **Closest competitor:** `Chickensoft.Log` + `Log.Godot` — engine-free core
  with a Godot writer, same architecture as ours. Rejected on fit: string
  Print/Warn/Err vs our structured event-name+fields, per-category
  verbosity, and correlation scopes.
- **Chickensoft.Serialization / SaveFileBuilder:** headline feature is
  serializing Godot types — which B44 saves deliberately never contain.
  Worth re-reading for AOT/polymorphism design ideas when the 1.0
  versioned-envelope work happens; not a dependency to take.
- **Godot-side "buy, don't build" pointers** (future gaps, not package
  concerns): in-engine scene testing → GdUnit4 / GoDotTest +
  GodotTestDriver; node-binding boilerplate → `[Node]` source generators
  (GodotUtilities, Chickensoft AutoInject) as the alternative to the
  `*Paths.cs` convention.
- Caveat applying to all of these: small-org projects whose cadence is
  chained to engine releases — the "Godot-side code churns with engine
  versions" bar applies to Godot-side dependencies too.

## Versioning & Publish

- `0.x.y` while the API churns; breaking changes bump the minor version.
- Publish = push a `v*` tag (e.g. `git tag v0.1.0 && git push origin v0.1.0`);
  `publish.yml` tests, packs with that version, and pushes to
  `https://nuget.pkg.github.com/DA-Sandman-Jr/index.json`.
- After publishing a breaking change, bump the `PackageReference` in each game
  deliberately — games pin exact versions.

## Layout

- `B44.Common/` — the package. Root namespace `B44.Common`; sub-namespaces
  mirror the games' old folder names (`Diagnostics`, `Interfaces`,
  `Persistence`) so migration was/is a mechanical namespace swap.
- `B44.Common.Tests/` — xunit.v3. `<TestingPlatformDotnetTestSupport>true`
  is required for `dotnet test` to discover xunit.v3 on current SDKs.
- `templates/` — canonical copies of the cross-repo standards (build props/
  targets, workflows, CLAUDE.md skeleton, nuget.config, test guard). Fix
  drift HERE first, then re-copy to game repos.

## Tests

```bash
dotnet test
```
