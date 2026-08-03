# B44.Common — Shared Engine-Free Primitives

<!-- B44 ORGANIZATION GUIDANCE: START -->
## B44 Organization Guidance

- `AGENTS.md` files are auto-generated from sibling `CLAUDE.md` by the opt-in `B44.Standards` build target. Edit the `CLAUDE.md`, not the `AGENTS.md`.
- Before editing or reviewing a file, read and follow every applicable `CLAUDE.md` from the repository root through that file's directory. Nearer instructions override broader instructions.
- Analyzer severities live in the `B44.Standards` packaged globalconfig, never in a repository `.editorconfig`. Repository editorconfigs own style and whitespace only; tune analyzer policy upstream in the package.
- Public server/function and endpoint-owning projects set `<B44SecuritySensitive>true</B44SecuritySensitive>` in `Directory.Build.props`; B44.Standards then enables the complete SDK Security category at a target-level-pinned rule set.
- Fix shared behavior in the B44 package that owns it; do not fork or paste a local copy into a consumer repository.
- Use compatibility-bounded floating versions for internal B44 packages in every consumer, including production: pre-1.0 packages use `0.<minor>.*`, while stable packages use `<major>.*`. Package owners bump the excluded boundary for breaking changes, and consumers cross that boundary manually. Never use an unbounded `*`. Enforcement-expanding Standards changes bump the minor version and never enter an existing patch float.
- Treat roughly 350 physical lines as a review warning for production source files. New production files should normally stay at or below 500 lines; files above 650 lines require a clear cohesion-based reason.
- Existing oversized files must not grow unless the same change performs a real extraction and leaves the file smaller. Coordinators coordinate; do not evade the limit with cosmetic partial classes, one-method services, generic utility dumping grounds, or needless factories.
- Before automated analyzer fixes, baseline measurement, scripted bulk text rewrites, or consuming a freshly published package, read `.b44/B44.Tooling.md`.
- Godot writes a `.uid` file beside every script as a stable identifier. Commit all of them and never add `*.uid` to `.gitignore`: the project still works locally without them, but references break as soon as it is cloned onto another machine, including a CI runner doing a fresh checkout. Godot generates them for every C# script under the project directory, including engine-free `Core` and test projects it never loads; that is expected and those files are committed too.
- Each repository keeps a root `BACKLOG.md` for agreed-but-not-started work and known defects, with defects in their own section so they stay distinct from planned work. It is authored by hand, never generated and never gated by the build — an empty file written to satisfy a check is worse than no file. Cross-repository programs live once in `B44.Common`'s backlog; a consumer's backlog links to the program and holds only its own share of the work, never a restatement that can drift.
- Isolation is by repository, not by folder. Engine- or framework-coupled adapters live in their own repository and package so engine-free build guards remain literal and release cadences stay independent.
- Keep licensing boundaries explicit. Source governed by terms different from a repository's `LICENSE` belongs behind a separately documented repository/package boundary with its provenance and required notices intact.
<!-- B44 ORGANIZATION GUIDANCE: END -->

Source and release home for the `B44.Common` NuGet package. B44-wide build and
agent policy is independently versioned in the public `B44.Standards` package
and repository.

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
  unreleased, unreadable saves throw `StoreException` and may be reset rather
  than format-migrated (after `AtomicJsonFileStore`'s automatic last-good
  `.bak` recovery). At each game's 1.0 this flips: released saves are a
  compatibility surface, and that game adds a versioned envelope + migration
  chain on top. The store itself stays format-agnostic either way.
- **Destructive policy is the game's call, never the factory's.**
  `RepositoryFactory.CreateWithFallback` takes a required
  `UnreadableSavePolicy`; there is no default. `Preserve` leaves unreadable
  bytes untouched and runs the session in memory, `Reset` deletes and stays
  file-backed. A shared factory must not decide on a game's behalf whether a
  player's save gets discarded — pre-release games may still choose `Reset`,
  but the choice has to be visible at the call site.
- **Determinism is API.** `SystemRandomSource` seeded sequences must match
  raw `System.Random` (tests pin this). Changing them breaks game test suites
  downstream.

## Persistence — Decision Record

`AtomicJsonFileStore` stays custom JSON-on-disk (reviewed against
LiteDB/SQLite/Akavache, 2026-07-16). One small human-readable document per
concern beats an embedded database here: no queries or partial updates exist;
JSON + System.Text.Json's tolerant deserialization makes additive save
evolution free and shape-breaking migrations a readable `JsonNode` transform;
and an embedded database adds a SECOND compatibility surface (LiteDB has
broken its own file format between majors) plus native-binary export friction
(SQLite). Durability concerns are answered in-store instead: flush-to-disk
before the rename, and `.bak` rotation with automatic recovery on load — the
tests pin all of it. Versioned-envelope/migration helpers land in this package
only when **two games need materially equivalent envelope behavior**, or a game
approaches released saves / 1.0. A game merely *having* a save version does not
trigger it — a local version field is game-owned and expected. (Refined
2026-07-30: the earlier wording keyed off a second game's envelope *existing*,
which Time Machine Clicker's planned save version would satisfy mechanically
while proving nothing about shared behavior.)

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

## Analyzer Scope Review — All-B44 (2026-07-17) & Flip Conditions

Measured the non-game repos before generalizing (async/threading/culture
greps): they are async HTTP-client apps with ZERO threading primitives, zero
sync-over-async, zero ambient DateTime, zero culture-risky formatting. Hence:
`MA0040` (forward in-scope CancellationToken) added for that profile;
everything else unchanged. Rejected-with-evidence, revisit only on the flip:

1. **Microsoft.VisualStudio.Threading.Analyzers** — zero threading exists
   anywhere. Flip: any repo introduces `Task.Run`/locks/a UI sync-context.
2. **Culture rules (`MA0011`/`CA1305`)** — zero risky formatting call sites.
   Flip: a repo starts producing parsed or culture-sensitive user strings.
3. **Security analyzers** — flipped for public/server code after the BFA audit
   (2026-07-21). `<B44SecuritySensitive>true</B44SecuritySensitive>` enables the
   complete built-in SDK Security category only for opted-in projects, pinned
   to their target-framework rule level. The initial BFA + endpoint-library
   baseline found one actionable CA5399 and no other diagnostics; games remain
   outside this profile to avoid irrelevant security-rule noise.
4. **PublicApiAnalyzers on B44.Common** — churn pre-1.0. Flip: first 1.0
   game ships against B44.Common as a released compatibility surface.

Adoption notes for the non-games: repos without a Core project (single-csproj
apps, servers) take the analyzer layer only — no `B44EngineFreeCore`;
`B44Deterministic=true` is free where measured (zero ambient time). Server
components (ASP.NET/Functions) keep their framework logging (MEL) — the
custom logger's decision record already scopes it to the games.

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
  `release.yml` tests and packs `B44.Common`, then publishes to nuget.org
  through Trusted Publishing (OIDC; no long-lived API key).
- **The tag is the package version.** `release.yml` derives `VERSION` from the
  tag and passes `-p:Version=$VERSION` to build and pack. The `<Version>` in
  `B44.Common.csproj` is a local hint that the release overrides; keep it
  matching the next intended tag so it does not mislead. `B44.Standards` and
  `B44.Templates` release independently from the `B44.Standards` repository.
- After publishing a breaking change, bump each consumer's compatibility
  boundary deliberately. Compatible releases flow through bounded floats.

## Layout

- `B44.Common/` — the package. Root namespace `B44.Common`; sub-namespaces
  mirror the games' old folder names (`Diagnostics`, `Interfaces`,
  `Persistence`) so migration was/is a mechanical namespace swap. Source-size
  enforcement is a build-time gate owned by `B44.Standards` through the
  `B44VerifyRatchet` / `B44WriteRatchetBaseline` target pair. No analyzer
  implements relative-to-baseline no-growth, which is why that target remains
  custom.
- `B44.Common.Tests/` — xunit.v3. `<TestingPlatformDotnetTestSupport>true`
  is required for `dotnet test` to discover xunit.v3 on current SDKs.
- `Directory.Build.props` — consumes the bounded `B44.Standards` package and
  opts this repository into synchronized guidance and the source-size ratchet.

## Tests

```bash
dotnet test
```
