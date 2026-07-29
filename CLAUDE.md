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
- Each repository keeps a root `BACKLOG.md` for agreed-but-not-started work and known defects, with defects in their own section so they stay distinct from planned work. It is authored by hand, never generated and never gated by the build — an empty file written to satisfy a check is worse than no file. Cross-repository programs live once in `B44.Common`'s backlog; a consumer's backlog links to the program and holds only its own share of the work, never a restatement that can drift.
- Isolation is by repository, not by folder. Engine- or framework-coupled code (`B44.Godot`, any future adapter) and third-party code we vendor, port, or convert each live in their own repository publishing their own package, never inside a normal B44 repository. Engine coupling keeps the engine-free MSBuild guard literally true with no carve-outs and decouples our cadence from the engine's. Third-party code is a licensing boundary: B44 packages are all rights reserved — source public for reference, not licensed for reuse — so an obligation-bearing file inside one contradicts its own terms. Converting or hand-porting does NOT shed the upstream license; a port is a derivative work and the attribution obligation follows it. Each such repository carries its own `LICENSE` and `THIRD-PARTY-NOTICES.md`. A separate project in-tree is not a substitute: it would require weakening the guard, or dual-licensing within one tree.

### Clean-Room Reimplementation — Protocol

When we need behavior an outside codebase has, but its code must not enter a B44 repository, it may be reimplemented clean-room. Output of a correctly executed clean room is B44's own expression — it is not third-party code under the isolation rule and needs no isolated repository. A clean room that skips any step below is not a clean room; its output is a port, and a port does.

1. **Two sides, two vendors.** The describing side may read the source. The implementing side must be a *different model from a different vendor* (Anthropic ↔ ChatGPT/Codex), in a separate session, with no shared context, no shared memory store, and no file access to the source. The implementer never sees the original, at any point, in any form.
2. **Only behavior crosses the wall.** The spec states inputs, outputs, invariants, edge cases, error conditions, and observable ordering guarantees. It must NOT contain source excerpts, pseudocode, function or type decomposition, identifier names, or internal step sequence. If someone could reconstruct the original's structure from the spec, the spec is itself a derivative work — rewrite it before it crosses.
3. **Review the spec before it crosses.** A human reads it for leaked structure. This is the control that actually matters; steps 1 and 5 are worthless if the spec is a transcription.
4. **Check the result for memorized expression.** Different vendors decorrelate training data but do not eliminate the risk — popular OSS sits in every corpus, so the implementer may reproduce upstream expression from weights rather than from the spec. Diff the implementation against upstream for verbatim runs before accepting it, and treat a hit as contamination rather than coincidence.
5. **Keep the record.** Retain the spec as it crossed the wall, which model/vendor produced each side, and the date. Without records the work was done but cannot be shown, and being able to show it is the point.

**Scope limits.** This addresses copyright only. It is no defense to patents (independent creation is irrelevant there) and does not cure license, EULA, or trade-secret obligations accepted in order to obtain the source. At step 1, do not paste confidential or non-public source into a third-party vendor's product; the describing side is only safe for source we are already entitled to read.

**Not worth it for permissive licenses.** MIT/BSD/Apache cost an attribution notice. Clean-rooming to avoid that is high effort and residual risk to dodge a trivial obligation — take the code and add the notice, in its own repository per the isolation rule. Reserve this protocol for copyleft (GPL/AGPL) or unlicensed/proprietary behavior we cannot take on its own terms.
<!-- B44 ORGANIZATION GUIDANCE: END -->

NuGet packages (`B44.Common` and `B44.Standards` on nuget.org) consumed by B44
repositories. This is also the canonical source for B44-wide build and agent
guidance distributed through `B44.Standards`.

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
and a third-party container adds a SECOND compatibility surface (LiteDB has
broken its own file format between majors) plus native-binary export friction
(SQLite). Durability concerns are answered in-store instead: flush-to-disk
before the rename, and `.bak` rotation with automatic recovery on load — the
tests pin all of it. Versioned-envelope/migration helpers land in this package
only when a second game needs them at 1.0 (one game's save-envelope
implementation is the first occurrence).

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
  `release.yml` tests and packs both packages, then publishes to nuget.org
  through Trusted Publishing (OIDC; no long-lived API key).
- After publishing a breaking change, bump each consumer's compatibility
  boundary deliberately. Compatible releases flow through bounded floats.

## Layout

- `B44.Common/` — the package. Root namespace `B44.Common`; sub-namespaces
  mirror the games' old folder names (`Diagnostics`, `Interfaces`,
  `Persistence`) so migration was/is a mechanical namespace swap. `Quality/`
  holds `SourceSizeRatchet`, now `[Obsolete]`: the ratchet is a build-time
  gate, so it lives in `B44.Standards` as the `B44VerifyRatchet` /
  `B44WriteRatchetBaseline` target pair. The type stays one minor for consumers
  to migrate off, then goes. No analyzer implements relative-to-baseline
  no-growth, which is why this is custom at all.
- `B44.Standards/` — build and agent policy as a package (analyzers via plain
  package dependencies, buildTransitive props/targets, canonical managed
  guidance under `guidance/`, `config/` globalconfigs +
  `BannedSymbols.Determinism.txt`/`BannedSymbols.Godot.txt` + `CodeMetricsConfig.txt`; determinism bans are usable by ANY B44 repo via B44Deterministic=true, Godot bans ride B44EngineFreeCore). Severity layering rule:
  repo `.editorconfig` owns style/whitespace ONLY — analyzer severities live
  in the packaged globalconfig, because `.editorconfig` outranks global
  configs and creates unoverridable conflicts (CA1861 taught us). Tuning
  changes go through this package, never per-repo editorconfigs.
  `MA0048` (one type per file) is deliberately NOT enabled; sanctioned
  multi-type files are B44 style. The source-size ratchet also lives here:
  `B44VerifyRatchet` runs on every build when `B44RatchetEnabled=true`, and
  `B44WriteRatchetBaseline` is a separate manual target hooked to nothing.
  Regenerating a baseline is an explicit act performed in the same change as a
  real extraction — a gate that can rewrite its own expectations during an
  ordinary build is not a gate. **An agent must never grant itself a ratchet
  exception** by raising a baseline entry or editing the configuration; when
  the build fails on the ratchet, do the extraction, or stop and ask David. `TreatWarningsAsErrors` is staged per repo
  AFTER its allowlist is tuned, not day one.
- `B44.Common.Tests/` — xunit.v3. `<TestingPlatformDotnetTestSupport>true`
  is required for `dotnet test` to discover xunit.v3 on current SDKs.
- `templates/` — bootstrap examples for new repositories (build props,
  workflows, local instruction skeleton, nuget.config, test guard). Ongoing
  organization/game guidance and synchronization come from B44.Standards;
  templates are not copied policy forks.

## Tests

```bash
dotnet test
```
