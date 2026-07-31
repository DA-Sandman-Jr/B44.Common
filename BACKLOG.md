# B44.Common / B44.Standards Backlog

Planned work that is agreed in principle but not yet scheduled into a release.
Decisions that are already settled live in [`CLAUDE.md`](CLAUDE.md) (hard rules
and decision records) — this file is for what is still ahead.

Status values: **Planned** (agreed, not started), **In progress**, **Blocked**,
**Done** (drop the entry at the next release and record it in `CLAUDE.md` or
the READMEs if it changed a rule).

Entries under [Planned](#planned) are owned by this repository. Multi-repository
programs are tracked under [Cross-repo programs](#cross-repo-programs) with the
owning repository named per wave, because no game repository has a backlog file
of its own today.

---

## Isolation boundaries — DECIDED

**Status:** Decided 2026-07-28 — **separate repository for both categories**.
Now a hard rule in [`CLAUDE.md`](CLAUDE.md); the rationale below is kept as the
decision record. Remaining open points are mechanics, not the boundary itself.

Two kinds of code must not be mixed into `B44.Common` / `B44.Standards`, for
different reasons — and both are isolated the same way, by repository.

**Category A — engine-coupled code** (`B44.Godot`, and any future engine or
framework adapter). Reasons to isolate: this repository's engine-free rule is a
hard rule enforced by an MSBuild guard; Godot-side code churns on the engine's
release cadence rather than ours; and consumers that are not games should never
resolve a GodotSharp reference transitively.

**Category B — third-party code we vendor, port, or convert** (MIT or any other
license). Reasons to isolate are legal, not architectural, and they are the
stronger of the two:

- `B44.Common` and `B44.Standards` are **all rights reserved** — source public
  for reference, not licensed for reuse. MIT code carries attribution and
  license-text redistribution obligations that follow the binary to every
  consumer. Mixing the two puts an obligation-bearing file inside a package
  whose stated terms grant nothing.
- **Converting or porting does not shed the license.** A hand-port of MIT
  source to C# is a derivative work; the attribution requirement follows the
  port. "We rewrote it" is not a boundary.
- A mixed package needs a per-file provenance story and a
  `THIRD-PARTY-NOTICES` surface that neither package has today. Keeping the
  boundary at the package/repo edge keeps that surface in exactly one place.

**Why repository and not just a separate project:** it makes the boundary
structural rather than a convention someone can erode with one `git mv`. The
engine-free MSBuild guard and the all-rights-reserved LICENSE both stay
literally true, with no carve-outs to maintain. A separate project in-tree
would require weakening the guard for Category A and dual-licensing within one
tree for Category B.

**Still open (mechanics only):**

- One shared repo per category, or one repo per dependency? Per-category is
  likely enough at current scale.
- Does CI verify a `THIRD-PARTY-NOTICES.md` entry exists for every vendored
  source, or is that review-only?
- Naming: is `B44.Godot` the pattern (`B44.<Adapter>`), and what is the
  Category B equivalent — `B44.Vendored.<Upstream>`, or the upstream's own
  name?
- Do Category B packages still take the `B44.Standards` analyzer layer, or
  are vendored sources analyzer-exempt to keep upstream diffs clean?

---

## Planned

### 1. Create `B44.Godot`: composition smoke testing, then the shared adapters

**Status:** Planned — **decided 2026-07-30 to build it.** This entry previously
recommended *not yet*, with a trigger of "a third distinct adapter, or ~150
lines." The Godot composition smoke harness is that third adapter, and it
carries the strongest shared-behavior argument of the three, so the trigger is
satisfied.

#### Why the CI lives here and not in `B44.Common`

A composition smoke test must assert that autoloads resolved and declared
NodePaths are valid. That is engine-side C# (`using Godot`), which `B44.Common`
cannot host — the engine-free rule is enforced by an MSBuild guard, and the
isolation rule puts engine-coupled code in its own repository. Only pure YAML
orchestration could live in `B44.Common`, and a YAML-only workflow can assert
little beyond "the process exited non-zero," which pushes the real validation
back into three per-game copies.

The workflow and the harness are also one contract in two respects: the success
marker the harness emits is what the workflow asserts on, and the Godot version
the workflow installs must match the GodotSharp the harness compiles against.
Nothing enforces either pairing across a repository boundary.

Resulting rule, worth stating once: **CI lives with the thing it tests.**
`B44.Common` keeps the engine-free .NET reusable workflow; `B44.Godot` owns the
Godot one.

#### 1A. Repository + smoke harness + reusable workflow

Do this first, and do not bundle the adapter migration into it.

- Own `LICENSE` and `THIRD-PARTY-NOTICES.md`, per the isolation rule.
- Cannot set `B44EngineFreeCore=true`. Record which `B44.Standards` enforcement
  it does adopt, and whether `B44Deterministic` applies.
- Pin the supported Godot/GodotSharp range and document the versioning policy.
  **Note what it is diverging from:** `B44.Common` and `B44.Standards` publish in
  lockstep from a single `v*` tag — `release.yml` derives `VERSION` from the tag
  and passes `-p:Version=$VERSION` to both. A third package either joins that
  scheme or needs its own tag convention; it cannot silently assume independence.
- Harness: observe a game-provided ready/failed startup state, validate required
  autoloads and declared NodePaths, detect startup exceptions and engine errors,
  emit one standardized success marker, exit with a deterministic code.
- The game lifecycle state and the harness marker are **not** the same
  abstraction. The game exposes ready/failed; the harness observes it; the
  harness emits the standard marker; the workflow validates marker and exit code.
- Workflow: `godot-version` required with no default (so each game owns its pin
  and this repository never needs editing on a Godot release); project path and
  smoke-test entry point as inputs; job-level timeout so a project that fails to
  exit fails the job rather than hanging the runner; verify the requested Godot
  version is compatible with the game project's `Godot.NET.Sdk` version.
- Leave `B44.Common`'s engine-free .NET workflow unchanged. Add no Godot
  dependency to any `B44.Common` assembly.
- Wire at least one game before calling it done — prefer Whispers if its
  startup-readiness work has landed, otherwise the simplest game first.
- **Serial dependency, plan for it:** `B44.Godot` must be created, packaged,
  published to nuget.org, and consumed before any game can adopt the harness —
  the same publish-then-migrate cycle the `B44.Standards` 0.8.x work used.

#### 1B. Migrate the shared adapters (after 1A works)

Second-occurrence gate — **measured 2026-07-29**, all three confirmed against
the repositories:

- **Godot logger sink — 3/3 games.** `TicTacHoe/Diagnostics/GodotLoggerFactory.cs`,
  `TimeMachineClicker/Diagnostics/GodotLoggerFactory.cs`, and Whispers'
  `Scripts/Diagnostics/StructuredGameLogger.cs` all map severity to
  `GD.PushError` / `GD.PushWarning` / `GD.Print`. Behaviorally identical; the
  diffs are an `if`-chain vs a `switch` and an expression body. TicTacHoe's file
  documents its own fork ("Mirrors `Whispers.Scripts.Diagnostics.GodotLoggerFactory`").
- **`NodePathValidator` — 2/3 games.** TicTacHoe (49 lines) and Whispers (48)
  differ only in namespace and one throw: a descriptive
  `InvalidOperationException` vs a bare `ArgumentNullException(property.Name)`.
  **Take TicTacHoe's.** Time Machine Clicker has no copy.
- The `GD.PushWarning` warning sink passed to
  [`RepositoryFactory.CreateWithFallback`](B44.Common/Persistence/RepositoryFactory.cs:7).

Scope it deliberately small: a thin adapter package over primitives that already
exist in `B44.Common`, not a second home for game logic.

**Do not reintroduce the GD0102 `global using` workaround.** Tested 2026-07-29:
Godot 4.7 marshals cross-assembly enums into `[Export]` properties correctly.
Verified by rebuilding Whispers with `EmitCompilerGeneratedFiles` and reading the
output — each `[Export] LogSeverity` property produces a complete entry in
`*_ScriptProperties.generated.cs`, marshalling through
`VariantUtils.ConvertTo<B44.Common.Diagnostics.LogSeverity>` and registering with
`PropertyHint.Enum`. The aliases are already removed from Whispers and the stale
notes in both games are corrected.

#### Hidden `/root` lookups — decided 2026-07-30

Do **not** build a `B44.Standards` first-party Roslyn analyzer for this yet. It
would be the package's first — today it ships only third-party analyzers plus
configuration — and that is a new maintenance surface. Whispers adds a
repository-local architecture test with an allowlist of approved composition
files instead (see its `BACKLOG.md`). Reconsider a shared analyzer only after
the same violation recurs in a second repository, which is the second-occurrence
rule applied as written.

### 2. Convert the bootstrap snippets into a real B44 game template

**Status:** Planned — **promoted to critical path 2026-07-31.** At four games a
year this is what makes the cadence real; see
[P2](#p2-reorganize-for-a-twelve-game-portfolio).

[`/templates`](templates/README.md) is currently a table of files to copy by
hand (`Directory.Build.props`, `format.yml`, `build-test.yml`, `nuget.config`,
`CLAUDE.skeleton.md`, the Godot-guard csproj snippet). Turn that into a real
template so a new game repository is one command instead of a checklist —
`dotnet new` template package is the obvious form.

Ongoing policy must keep flowing from `B44.Standards`; the template seeds a
repository once and must not become a copied policy fork. Anything in the
template that would need to change when policy changes is a sign it belongs in
`B44.Standards` instead.

**Open questions to resolve before starting:**

- Ship the template inside `B44.Standards`, or as a separate
  `B44.Templates` package?
- How much does it scaffold — repository configuration only, or also the
  engine-free Core project and test project with the Godot guard wired up?
- What is the placeholder substitution surface (today's `GAME` placeholders in
  `build-test.yml`)?

### 3. Promote the source-size ratchet gate into `B44.Standards`

**Status:** **Done 2026-07-29.** Shipped in `B44.Standards` 0.8.1 and adopted by
all three games. Zero forked `ArchitectureRatchetTests.cs` remain in the
portfolio. Drop this entry at the next release.

Two things surfaced during consumer migration that the plan had not anticipated:

- **Time Machine Clicker had a third fork.** The source handoff named only
  TicTacHoe and Whispers; TMC carried the same file. Migrated with the others.
- **Regeneration destroyed per-entry `# reason` comments** — found on TMC's
  baseline, which documents each exception inline. That is the exception
  mechanism, not decoration, so a regeneration performed for an unrelated
  extraction would have erased every other entry's justification. Fixed in
  0.8.1; regeneration now carries comments across, preserving spacing so a
  no-op regeneration stays byte-identical. The deprecated
  `SourceSizeRatchet.WriteBaseline` had the same flaw and merely documented it.

**Whispers' baseline values were deliberately left alone.** A full regeneration
there would also tighten four entries and track one new file, because its
sources drifted since the baseline was last written — which is precisely the
defect this entry fixed, since no regeneration entry point existed. Every drift
is a shrink or a new sub-500 file, so none is a violation. Baselines move only
in a commit that performs a real extraction, so that regeneration is left as a
separate deliberate call.

**Shipped:**

- `B44VerifyRatchet` (runs `BeforeTargets="PrepareForBuild"` on the configured
  anchor project) and `B44WriteRatchetBaseline` (hooked to nothing, invoked
  explicitly), both driving one `B44SourceSizeRatchetTask` inline task —
  the same `RoslynCodeTaskFactory` shape as `B44SyncAgentGuidance`.
- `B44Ratchet*` property block plus a `B44RatchetExclude` item group, so the
  exclude list is single-sourced in `Directory.Build.props` and read by both
  verify and write. This was the actual coupling defect: an exclude list living
  inside each forked test file is why a shared regenerator was impossible.
- Baseline writing preserves the existing file's line-ending style. The
  portfolio is not consistent here — Whispers' baseline is LF while TicTacHoe's
  and TMC's are CRLF — and a regeneration that rewrites every line is
  indistinguishable from one that changed a value.
- A malformed baseline entry hard-fails (`B44R001`) instead of degrading to an
  empty baseline, which would disable the gate with nothing looking wrong.
- Dogfooded: this repository now runs the ratchet on itself, with a
  `ratchet-baseline.txt` recording zero tracked files.
- `B44.Standards.Ratchet.Tests`, a build-only fixture mirroring
  `B44.Standards.AgentGuidance.Tests`, with six assertions: exclusions and
  `bin`/`obj` skipping, baseline contents, write→verify round trip, growth past
  a baseline, shrink under a baseline, and the malformed-baseline failure.
- `B44.Common`'s `SourceSizeRatchet` is `[Obsolete]` pointing at the target,
  and goes at the next minor. Its tests stay green behind a `#pragma` so the
  deprecated path cannot rot while consumers migrate.

**Verified by direct experiment, not inspection:**

1. Padding a tracked file to 539 lines failed the build with a message naming
   the file, its line count, and the reason. Reverted; build green.
2. Regeneration is idempotent — running `B44WriteRatchetBaseline` twice on an
   unchanged tree produced byte-identical output (same MD5).
3. Full solution build and the 56-test suite pass with zero warnings.
4. `dotnet format --verify-no-changes` passes **on an LF checkout**. It reports
   ~1470 spurious `ENDOFLINE` errors locally, but that is a pre-existing
   `core.autocrlf=true` artifact present at `HEAD` before any of this work and
   absent in CI — confirmed by checking a clean worktree both ways. Written up
   in `B44.Tooling.md` so the next person does not "fix" it with a
   repository-wide line-ending rewrite.

**One deliberate baseline change when consumers migrate:** the generated header
line changes from `# Regenerated via SourceSizeRatchet.WriteBaseline ...` to one
naming the new command. That is a comment, not a tracked value, so it does not
violate the "baselines change only in an extraction commit" rule — but it does
mean the first regeneration in each game produces a one-line diff. Expect it.

**Original problem statement, retained for context:** — blocked on one user decision (see below).

[`SourceSizeRatchet`](B44.Common/Quality/SourceSizeRatchet.cs) exposes both
`Check(...)` and `WriteBaseline(...)`, but consumers only reach the first, and
they reach it through a forked file. Two defects follow.

**The gate is copy-pasted into every consumer.** `TicTacHoe.Tests/ArchitectureRatchetTests.cs`
and `WhispersOfTheEarth.Tests/ArchitectureRatchetTests.cs` are the same 37-line
file; only three tokens differ (namespace, the `excludeDirs` entry naming the
test project, and the repo-root marker `.sln`). The `FindRepoRoot` parent walk,
the `Check` call, and the violation-joining assert message are identical line
for line. That is the fork the organization rule forbids — fix shared behavior
in the package that owns it.

**There is no entry point to regenerate a baseline.** Neither consumer contains
a call to `WriteBaseline`, yet both `ratchet-baseline.txt` files carry the
header `# Regenerated via SourceSizeRatchet.WriteBaseline after sanctioned
extractions.` — instructing an operation neither repo can perform. A sanctioned
extraction today means hand-writing a throwaway `WriteBaseline` caller, running
it, and deleting it; that is literally what happened in TicTacHoe on 2026-07-28
during the `FarmAi` split (`f56a3d8`). Whispers carries an 861-line
`Scripts/Scenes/Dungeon/DungeonController.cs` in its baseline, so it has more
extraction ahead and will hit this harder.

The two defects are coupled: `excludeDirs` lives *inside* each forked test file,
so any regenerator would have to duplicate the list — and if the copies drift,
the regenerator writes a baseline the gate then rejects.

**Shape to build.** `B44.Standards` already solves this problem for a different
file: the `B44SyncAgentGuidance` target in
[`B44.Standards.targets`](B44.Standards/buildTransitive/B44.Standards.targets)
maintains a checked-in file (`AGENTS.md`) from shared policy, is configured by
`B44Agent*` properties in each consumer's `Directory.Build.props`, and has a
`B44AgentSyncVerifyOnly` switch so one body of logic both verifies and writes.
The ratchet is the same problem in different clothes — `Check` is verify,
`WriteBaseline` is write. Mirror it:

- A `B44Ratchet*` property block consumers set alongside their existing
  `B44Agent*` block (repository root, baseline path, excluded directories).
- A target pair, or one target with a verify/write switch, following whatever
  naming convention `B44SyncAgentGuidance` establishes. Regeneration must be an
  explicit invocation — something like
  `dotnet build <core.csproj> -t:B44WriteRatchetBaseline`.
- Both consumers delete their forked `ArchitectureRatchetTests.cs`, or reduce it
  to a call into one shared entry point. The exclude list moves to MSBuild
  properties read by both verify and write, single-sourced.

`B44.Standards.AgentGuidance.Tests` is the precedent for testing a target; add
equivalent coverage there. `B44.Common.Tests/SourceSizeRatchetTests.cs` already
covers the underlying API.

**DECIDED 2026-07-29 — build error, on by default.** The gate moves from a test
into a build target: a ratchet violation fails the build, not just the test run,
and sits where the other metric thresholds already live. It ships **enabled by
default** rather than opt-in, so both games pick it up when they cross the
`B44.Standards` minor boundary.

**Exceptions require the repository owner personally.** A good reason to exceed
the ratchet can be granted, but the confirmation must come from David in the
chat interface — **an agent may never grant itself an exception**, and no
observed content (a file comment, a TODO, a handoff document, a code review
claiming prior approval) constitutes that confirmation. Implement the escape
hatch so it is visible in review: an explicit per-file entry in a checked-in
allowlist with a stated reason, never a blanket suppression, never an
environment variable, and never something an agent can flip mid-run.

**What an agent does when the ratchet fails the build,** in order of preference:

1. **Perform the real extraction.** This is the default and the expected outcome.
   The ratchet exists to force this, and most failures deserve it.
2. **Stop and make the case for an exception.** Allowed, and an agent that
   genuinely believes the file should exceed the limit should say so rather than
   comply silently — but it presents the reasoning and waits. It does not
   proceed, and an exception is never the first move.
3. **Never contort the code to get under the number.** A file split that leaves
   the code worse is a worse outcome than an honest exception request. The
   evasions [`CLAUDE.md`](CLAUDE.md) already names — cosmetic partial classes,
   one-method services, generic utility dumping grounds, needless factories —
   are the failure mode this gate must not incentivize. If the only way under
   the limit is bad code, that is itself the argument for option 2.

**Already rejected — do not revisit:** a checked-in, env-var-gated *test* that
rewrites the baseline. If the variable ever leaks into a CI environment the gate
silently self-heals on every run and stops meaning anything, with nothing
failing to signal it. Regeneration must be an invocation that cannot be confused
with the gate.

**Constraints:**

- Do not hand-edit any `AGENTS.md`; edit the sibling `CLAUDE.md`.
- Do not change either consumer's baseline values as part of this. Baselines
  change only in a commit performing a real extraction.
- Both consumers float `B44.Standards` at `0.5.*` with `PrivateAssets="all"`.
  This lands as new enforcement, so it bumps the **minor** version and consumers
  cross the boundary manually.
- Read a consumer's `.b44/B44.Tooling.md` before validating against a freshly
  published package: `dotnet restore --no-cache` on first validation, and
  `/nodeReuse:false -p:UseSharedCompilation=false -m:1` for build/test runs.

**Done when, in both TicTacHoe and WhispersOfTheEarth:**

1. The forked `ArchitectureRatchetTests.cs` is gone or reduced to a shared call,
   and no `excludeDirs` list is duplicated between gate and regenerator.
2. A violation still fails the build or suite with a message naming the file,
   its line count, and the reason (prove it: pad a tracked file, confirm the
   failure, revert).
3. Regenerating the baseline is a single documented command producing a file
   byte-identical to the committed one on a clean tree (prove it: `git diff`
   empty).
4. Full suite and `dotnet format --verify-no-changes` pass.
5. Each repo's `CLAUDE.md` ratchet paragraph names the new command, replacing
   the current "Regenerate the baseline via `SourceSizeRatchet.WriteBaseline`"
   prose that describes an operation with no entry point.

### 4. Standardize a `BACKLOG.md` across B44 repos

**Status:** In progress — decided and implemented in this repository 2026-07-29
(guidance bullet added to `B44.Organization.md`, `ROADMAP.md` renamed,
`README.md` updated). Remaining: every B44 consumer without a root
`BACKLOG.md` adopts it when next worked on, whether it is a game, library, or
hosted application. BeforeForeverAfter adopted it 2026-07-30 alongside its
private-package restore follow-up.

Every B44 repository accumulates "agreed but not started" work and "known broken,
not yet fixed" defects. Neither has a standard home, so both land wherever the
session that produced them happened to be looking.

**Surveyed 2026-07-29:**

| Repo | Backlog-shaped docs | Known-issues doc |
|---|---|---|
| B44.Common | `ROADMAP.md` (renamed to [`BACKLOG.md`](BACKLOG.md) by this entry) | none |
| TicTacHoe | `docs/expansion-plan.md`, `docs/gap-assessment.md` | none |
| Whispers | `docs/design/`, `docs/handoffs/` | none |
| Time Machine Clicker | `docs/ART_DIRECTION.md` only | none |

Four repositories, four conventions, no known-issues file anywhere. TicTacHoe's
two documents are backlog content under other names.

**Shape:** one root-level `BACKLOG.md` per repository, holding both planned work
and known defects, named as a convention in
`B44.Standards/guidance/B44.Organization.md` so it reaches every repo through the
package. Keep bugs in their own section within the file so they stay greppable
and visibly distinct from agreed-but-not-started work.

**Deliberately not machinery:**

- **Not generated like `AGENTS.md`.** That file is generated because it has an
  upstream source (the sibling `CLAUDE.md`). A backlog has no upstream; a sync
  target could only overwrite the author or fight them.
- **Not existence-gated in the build.** An empty file created to satisfy a check
  is worse than no file, because it looks like an answer.

**Single-sourcing rule:** cross-repo programs stay in this repository's
`BACKLOG.md` (see [Cross-repo programs](#cross-repo-programs)). A game repo's
backlog *links* to a program and holds only its own share of the work. Restating
a program in four files guarantees four drifting copies.

**Migration:** fold or link TicTacHoe's `expansion-plan.md` and
`gap-assessment.md`. Leave Whispers' `docs/handoffs/` alone — received documents
are a genuinely different artifact from a backlog, and P1's source handoff is one
of them.

#### Decided 2026-07-29

- **`BACKLOG.md`, not `ROADMAP.md`.** "Roadmap" implies sequencing that most of
  this content does not have.
- **No separate known-issues file.** Bugs go on the backlog. A second tracker is
  a second thing to keep current.
- **Ships as a patch.** This adds guidance text, not enforcement, so it does not
  need the enforcement-expanding minor bump; repos pick it up whenever they next
  restore.

**Consequence for this repository — done 2026-07-29:** the convention renamed
this file `ROADMAP.md` → `BACKLOG.md` (via `git mv`, so history follows) and
updated the one reference in [`README.md`](README.md).

**All three games adopted `BACKLOG.md` on 2026-07-30**, alongside
BeforeForeverAfter earlier that day. Notes from doing it:

- Time Machine Clicker already had one, product-focused, with its own
  `## Planned Work` / `## Known Defects` headings. The architecture entries were
  **appended** to it rather than replacing it. Those headings are now the
  convention — the two new files were aligned to them, not the reverse.
- Whispers had a `BACKLOG.md` that was deliberately deleted in `4eae4b6` as
  "outdated or empty" (it was three lines). The new one is not a resurrection of
  removed content.
- Whispers' `docs/handoffs/` left as-is, as intended.

**Pre-existing backlog-like documents folded 2026-07-30.** Surveyed everything
`.md` in the three games and folded only what was a genuine competing list of
open work:

- **Whispers `docs/design/gap-backlog.md` → folded and removed.** It was a second
  live backlog — its own header instructed readers to "fold them in here so
  nothing is lost across sessions," which is exactly the drift this convention
  prevents. Its open items moved under gameplay/UX/release-readiness headings,
  its source provenance and shipped-work record moved to Notes, and the four
  references to it in `agent-task-prompts.md` were repointed.
- **TicTacHoe `docs/gap-assessment.md` → gaps folded, file kept.** It was half
  scope-reference and half open work. The engineering and product gaps moved to
  the backlog; the file remains as a scope and status record with a pointer.
- **Left alone as design records, specs, or reference:** TicTacHoe's
  `expansion-plan.md` (every phase in its status table is `done`),
  `three-player-design.md`, `SCREEN_LAYOUT.md`; Whispers'
  `item-instance-refactor.md` (shipped), `docs/design/mechanics/` specs,
  `progression-enemy-ai-analysis.md`, and `agent-task-prompts.md` (task detail
  companion, not a competing list); TMC's `ART_DIRECTION.md`.

The distinction applied throughout: a document listing *what is still open* is a
backlog and gets folded; a document describing *what something is or was decided
to be* is a record and stays.

**Remaining:** any B44 consumer without a root `BACKLOG.md` adopts it when next
worked on, whether game, library, or hosted application. Not blocking — the
guidance describes a convention, and a repo adopts it the next time someone is in
there.

### 5. Make destructive save policy an explicit game choice — DONE 2026-07-29

**Status:** Done in `B44.Common` 0.7.0. Severed from [P1](#p1-portfolio-persistence-framework),
which is otherwise deferred; this piece was a live defect in shipped shared code
rather than framework groundwork, so it did not wait.

**The defect.** `RepositoryFactory.CreateWithFallback` probed the load path and,
on `StoreException`, called `Clear()` — a generic shared factory silently
deciding to discard a player's save. Pre-release that is a defensible *policy*,
but it was invisible: nothing at a consumer's call site said deletion could
happen, and a game reaching 1.0 would have inherited it by default.

**What shipped:**

- New `UnreadableSavePolicy` enum — `Preserve` (leave the bytes untouched, run
  the session on an in-memory store) and `Reset` (delete, stay file-backed).
- `CreateWithFallback` now takes it as a **required** parameter, positioned
  before the optional warning sink. There is deliberately no default value: a
  default is how the old behavior stayed invisible, and every call site should
  have to state which it wants.
- `Preserve` never calls `Clear()` at all, and the in-memory fallback means a
  session's saves cannot overwrite the preserved bytes either. Both are pinned
  by tests, since "preserve" that still loses the file on the next save would be
  worthless.
- Tests: 8 cases in `RepositoryFactoryTests`, covering both policies against
  healthy stores, uncreatable stores, corrupt saves, and un-clearable stores.

**Consumer impact — none yet.** All three games float `B44.Common` at `0.5.*`
and the package was already at `0.6.0`, so they sit below a boundary they have
not crossed. When a game does cross to `0.7.*` it gets a compile error at its
`CreateWithFallback` call site and must name a policy. `Reset` reproduces
today's behavior exactly; that is the right choice for all three while they are
pre-release, but it should be typed out rather than inherited.

**Follow-up when consumers cross:** TicTacHoe's `CampaignProgressServiceFactory`
and Time Machine Clicker's `GameStateRepositoryFactory` are the two call sites.
Whispers does not use `RepositoryFactory` at all.

### 6. Widen the pre-1.0 version float from `0.<minor>.*` to `0.*`?

**Status:** Planned — widening itself is recommended against (evidence below),
but the notification half is **promoted to critical path 2026-07-31**: twelve
games multiply every manual boundary crossing by four. See
[P2](#p2-reorganize-for-a-twelve-game-portfolio).

[`CLAUDE.md`](CLAUDE.md) has pre-1.0 packages consumed at `0.<minor>.*`
(`0.5.*`), so a minor bump is a boundary each consumer crosses by hand. The
proposal is to move the wildcard up a level to `0.*`, letting consumers pick up
minor releases automatically. This stays bounded, so it does not violate the
"never use an unbounded `*`" rule — but it does remove the protection that rule
exists to provide.

**The tradeoff is not the same for both packages, and the entry should probably
split them:**

- **`B44.Common`** — a library. Floating `0.*` is defensible: a breaking API
  change surfaces immediately as a compile error in the consumer that hits it,
  it is local to whoever is working there, and pre-1.0 churn makes the manual
  crossing pure toil.
- **`B44.Standards`** — enforcement. This is the package whose entire job is to
  change what fails your build. Floating it means publishing an
  enforcement-expanding release (a new analyzer severity, or
  [entry 3](#3-promote-the-source-size-ratchet-gate-into-b44standards) turning
  the ratchet into a build error) breaks the build in every game the next time
  it restores — on a day chosen by the publish, not by the person who then has
  to deal with it, and while they are working on something unrelated.

The current rule's value is not *whether* the break happens but *when*, and only
`B44.Standards` can break a repository that changed nothing. **Recommendation:
widen `B44.Common` to `0.*`, keep `B44.Standards` at `0.<minor>.*`.** That
removes most of the toil while leaving the deliberate gate on the package that
needs it.

Counter-argument worth weighing: the same person owns both sides of every one of
these boundaries, so "consumers cross manually" is one developer crossing their
own boundary, and a rule that only ever inconveniences its author may not be
carrying its weight.

#### Evidence from the 0.8.x releases (2026-07-29/30)

[Entry 3](#3-promote-the-source-size-ratchet-gate-into-b44standards) was the
concrete test case this entry was waiting on, and it ran. Both halves of the
current scheme behaved as designed:

- **The minor crossing was cheap.** Three games went `0.5.* -> 0.8.*` at roughly
  two lines each. The stop was not expensive; it was just a stop.
- **The patch flowed silently.** 0.8.0 -> 0.8.1 (the reason-comment fix) and
  0.8.2 reached every consumer with no action at all.

**Revised recommendation: do not widen either package.** The stop cost almost
nothing and it landed on exactly the change that alters what fails a build.

**Because the real cost was never the edit — it was noticing.** `B44.Standards`
sat at 0.7.0 in-tree, bumped but never tagged or released, and nobody spotted it
until a consumer restore failed with "nearest version 0.6.0". Widening the float
would not have helped with that at all; it is a notification problem wearing a
versioning problem's clothes.

**Candidate fix, untested:** Dependabot on the NuGet ecosystem in each consumer,
which opens a PR when a new minor falls outside the repo's current float. The
deliberate gate survives — a human still reviews and merges — while the "did I
miss a release" burden goes away. The caveat is real: Dependabot's NuGet support
is uneven with wildcard versions like `0.8.*`, and it may not fire at all on a
constraint that is already satisfiable. Verify on one repository before rolling
it out. If it will not cooperate, a small scheduled workflow in this repository
comparing each consumer's pinned boundary against the latest published version,
opening an issue on drift, does the same job with more control and no dependency
on Dependabot's wildcard behavior.

---

## Cross-repo programs

### P1. Portfolio persistence framework

**Status:** Deferred (2026-07-29) — the envelope and migration framework does
**not** get built ahead of 1.0; the second-occurrence rule and the "at 1.0"
clause in [`CLAUDE.md`](CLAUDE.md) stand unchanged. Waves 1, 3, 4, and 5 wait for
the first game approaching a compatibility promise. Two pieces are severable and
tracked separately below: the destructive-policy fix (ready now) and Wave 2,
which never depended on the framework.

**Source:** `GAP-ANALYSIS-AGENT-HANDOFF-REVISED.md` (received 2026-07-29). It was
written against commit pins, but this program targets the **current** state of
Whispers, Time Machine Clicker, Tic Tac Hoe, and B44.Common — inspect each repo
at implementation time. Where current code contradicts a finding below, the code
wins: document the discrepancy and adjust the wave rather than forcing the
recommendation onto the codebase.

**Objective.** One recognizable persistence lifecycle across every B44 game
without a shared payload schema. B44 owns the recurring infrastructure — minimal
outer envelope, framework-version vs payload-version separation, owning-game and
codec identification, durable writes, previous-good recovery, classified load
results, ordered migration mechanics, explicit policy selection, compatibility-test
support. Each game keeps its payload and snapshot schemas, capture and restoration,
validation and normalization, semantic migration transformations, supported
historical versions, compatibility promise, and the decision to preserve, migrate,
reset, or reject player data.

**The envelope must stay minimal.** It is not a universal game-state payload and
not a container for unrelated game features. The full non-goal list in the source
handoff (no universal payload schema, no ECS, no command bus, no replay/rollback,
no cross-game content registries, no B44-owned progression or action-result
abstractions) restates rules this repository already holds; nothing there is new
policy.

#### Rule conflicts — settle before Wave 1

The handoff is broadly compatible with existing B44 rules, but three points
change settled decisions and cannot be absorbed silently.

1. **DECIDED 2026-07-29 — no.** Timing vs the second-occurrence rule.
   [`CLAUDE.md`](CLAUDE.md) defers
   versioned-envelope and migration helpers to "when a second game needs them at
   1.0," naming one game's save-envelope implementation as the first occurrence —
   that is Whispers' [`Whispers.Core/Saves/ISaveEnvelopeStore.cs`](../WhispersOfTheEarth/Whispers.Core/Saves/ISaveEnvelopeStore.cs),
   confirmed present. Wave 1 builds the framework *before* any game has a
   compatibility promise. The handoff is explicit that this is intended
   ("the framework may be prepared for migrations before release"), and three
   games adopting it arguably is the second occurrence. But bringing it forward
   from 1.0 is a deliberate change to a decision record, not a side effect of
   implementation. **Decide, then amend `CLAUDE.md` in the same commit.**
2. **Serializer-neutral storage vs the persistence decision record.** The record
   settles `AtomicJsonFileStore` as custom JSON-on-disk; the handoff requires text
   *and binary* payloads to share one durability and recovery path. That is not
   the LiteDB/SQLite question the record answered, so it does not reopen it — but
   it does widen the store's contract, and the record should say so afterwards.
3. **Explicit destructive policy — accurate finding, breaking change.**
   [`RepositoryFactory.cs:32-49`](B44.Common/Persistence/RepositoryFactory.cs:32)
   does couple storage fallback with unreadable-save deletion, exactly as the
   handoff describes: the load probe catches `StoreException` and calls `Clear()`.
   Today that is documented pre-release behavior; the handoff requires the reset
   to become an explicit, visible game-level choice. Pre-release consumers may
   still opt in. This is an API break → **minor** bump, and both game consumers
   cross the `0.5.*` boundary manually.
   **Resolved 2026-07-29** — severed from this program and shipped in
   `B44.Common` 0.7.0; see
   [entry 5](#5-make-destructive-save-policy-an-explicit-game-choice--done-2026-07-29).

Also reconcile with [Planned entry 1](#1-add-the-small-b44godot-package-migrate-the-logger-sinks-and-nodepathvalidator):
that entry treats the logger sinks and `NodePathValidator` as already past the
second-occurrence gate and schedules the package now, while the handoff (§4.5,
Wave 5) treats `B44.Godot` as a conditional destination to be populated only
after equivalent stable behavior is proven in two real consumers, and defers it
to last. The gate is the same rule; the verdict on whether it is already met is
not. The roadmap should not carry both readings — settle it and edit entry 1.

#### Waves

Each wave is independently reviewable and releasable. Do not merge them into one
undifferentiated refactor.

| Wave | Owning repo(s) | Outcome |
|---|---|---|
| 1 | **B44.Common** | Common envelope and version boundaries; serializer-neutral durable storage with previous-good recovery; classified load results; destructive policy separated from storage fallback; shared compatibility-test support; the simple ordered migration boundary with no game transformations. No game state, no Godot dependency. |
| 2 | **Whispers** | One documented capture-and-quiescence contract; runtime / committed-state / snapshot / presentation boundaries documented. Independent of Wave 1 and may run in parallel. |
| 3 | **Whispers** + B44.Common | Whispers onto the shared framework; unsupported future data rejected *and preserved*; previous-good recovery; compatibility and restoration coverage. |
| 4 | **Time Machine Clicker**, **Tic Tac Hoe** | Adopt the framework; make each pre-release data policy explicit; preserve existing canonical state ownership; scaled compatibility and recovery coverage. |
| 5 | all consumers | Game-owned migrations where compatibility is actually promised; historical fixtures; scaled Godot-aware verification lanes; export integration; `B44.Godot` extraction only if the repeated-use gate is met. |

**Wave 1 is the only wave this repository implements.** Waves 2–5 are recorded
here for sequencing only; when a game repo gains its own backlog file, move its
waves there and leave a pointer.

**Starting-state notes gathered while filing this** (verify at implementation
time, do not trust these as current):

- Whispers references `B44.Common` `0.5.*` but has **zero** references to
  `AtomicJsonFileStore` or `RepositoryFactory` — it persists entirely through its
  own `Whispers.Core/Saves/` surface. "First substantial consumer" in the handoff
  means first, literally.
- Time Machine Clicker and Tic Tac Hoe both go through `RepositoryFactory`
  (`GameStateRepositoryFactory`, `CampaignProgressServiceFactory`), so conflict 3
  above is what actually reaches them.

#### Outcome bar

Portfolio-level, from the source handoff — the program is done when: every game
uses one recognizable lifecycle without sharing payload meaning; framework and
payload versions are independently understandable; text and binary payloads share
durability and recovery; unsupported future data is rejected and preserved;
previous-good recovery is available to every participating game; incompatibility,
invalid data, storage unavailability, recovery, and migration failure are not
collapsed into one corruption path; destructive behavior is explicitly selected
per game; migrations run through a consistent ordered process with game-owned
transformations; compatibility tests follow a recognizable shared pattern;
Whispers has one documented and tested capture contract whose pause, focus-loss,
transition, teardown, and shutdown paths never capture partially resolved state;
TMC keeps its canonical-state direction; Tic Tac Hoe stays campaign-only;
engine-independent verification stays separate from the Godot-aware lane; and
`B44.Godot` contains only twice-proven behavior, or is never created.

Per-wave reporting expectation: repository evidence confirming or contradicting
the finding the wave rests on, decisions made inside the handoff's boundaries,
behavior preserved for compatibility, development data that may be reset or need
manual handling, verification added, remaining release risks, and any proposed
expansion of shared scope with the concrete second consumer that justifies it.

---

### P2. Reorganize for a twelve-game portfolio

**Status:** Planned — structure agreed 2026-07-31, sequencing and two visibility
questions open. Nothing starts until [B44.Godot's naming question](#the-naming-decision)
is settled, because it blocks a publish.

**Why now.** Twelve more games are planned at roughly four a year. The current
shape — one shared repo with two packages, plus `B44.Godot` — was sized for
three games. Two things change at twelve: shared game-domain code becomes
genuinely reusable rather than speculative, and *coordination* becomes the
binding constraint rather than code reuse.

#### The organizing principle

**One repository per boundary that cannot be crossed — not one per package.**
Exactly three things force a repository split:

1. **Engine coupling** — the engine-free MSBuild guard must stay literally true.
2. **Licence obligation** — see the refinement below.
3. **Visibility** — public portfolio surface versus private work.

Package count forces nothing. `B44.Common` and `B44.Standards` already ship two
packages from one repository, lockstep from a single tag. That is the lever that
keeps twelve games tractable.

#### Target structure

| Repository | Ships | Split reason |
|---|---|---|
| `B44.Standards` | `B44.Standards` | Public — portfolio surface |
| `B44.Common` | `B44.Common` | Engine-free mechanisms |
| `B44.Games` | `B44.Games.Inventory`, `B44.Games.Dungeons` | Engine-free game-domain |
| `B44.Godot` | `B44.Godot`, `B44.Godot.Inventory` | Engine-coupled |
| `B44.Vendored` | per-upstream packages | Obligation-bearing third-party |

Dependency direction: `B44.Common` → `B44.Games.*` → games, with `B44.Godot`
adapting each engine-free layer and `B44.Vendored` consumed directly.

**The trade-off, stated plainly.** Lockstep within a repository means
`B44.Games.Dungeons` takes version bumps it did not need when `Inventory`
changes. That buys five trusted-publishing policies instead of nine, five
`NUGET_USER` secrets, five CI pins, and four package floats per game instead of
eight — times twelve games. At three games the split would have been right; at
twelve the coordination cost dominates.

#### The naming decision

Keep **`B44.Godot`**, not `B44.Games.Godot`. Read the scheme as
`B44.<layer>.<domain>`: `B44.Godot` is the engine layer, `B44.Godot.Inventory`
is a domain within it. Putting the engine *under* a games layer misnames the
smoke harness, which is generic engine plumbing rather than game-domain code.

Practical consequence: no rename, so no stranded package ID, and the pending
`B44.Godot` publish is unblocked the moment its trusted-publishing policy exists.

#### Rule refinement — obligation, not origin

The isolation rule currently keys off "third-party code." That is the wrong
test. **The test is whether the material carries an obligation.**

- **MIT/BSD/Apache** — attribution and licence text travel with the binary to
  every consumer. Obligation-bearing, so it cannot sit inside an all-rights-
  reserved package without contradicting that package's own terms. Isolated
  repository, own `LICENSE` and `THIRD-PARTY-NOTICES.md`.
- **CC0** — a public-domain dedication. Nothing to carry, nothing to
  contradict. May live in a normal repository. Record provenance anyway —
  source, date, what was taken — for the audit trail and for storefront
  disclosure, not because a licence demands it.

Fold this wording into `B44.Organization.md` when the program starts.

#### Vendored repository shape

Start with **one** `B44.Vendored` repository, one directory per upstream, one
`THIRD-PARTY-NOTICES.md` listing each. Split to per-upstream repositories only
when an upstream needs its own release cadence. Per-upstream is cleaner in
theory but pays the repo + policy + secret + pin cycle per upstream, and licence
notices are small enough to bundle. This answers one of the open mechanics in
[Isolation boundaries](#isolation-boundaries--decided).

#### Extraction scope

The second-occurrence rule is now satisfied by planned consumers rather than
existing ones — "demonstrably will within the current effort." Verified
2026-07-31 that **no** inventory or dungeon code exists in TicTacHoe or Time
Machine Clicker today, so this rests entirely on the twelve-game plan. If that
plan changes, revisit before extracting.

- Whispers' engine-free inventory → `B44.Games.Inventory`
- Whispers' multi-floor dungeon continuity → `B44.Games.Dungeons`
- The Godot-side inventory layer → `B44.Godot.Inventory`, after the engine-free
  core is stable

**Sequencing constraint:** do not extract Whispers' inventory or dungeon code
until its startup-readiness and `/root` work land and P1 Wave 2 defines the
lifecycle seams. Those are the same files. Extracting first means extracting to
seams that are about to move.

#### These two entries are now critical path

At twelve games the bottleneck is repo setup and version coordination, so two
entries previously treated as low-urgency become the things that actually buy
the cadence:

- **[Entry 2, the game template](#2-convert-the-bootstrap-snippets-into-a-real-b44-game-template).**
  A new game currently costs: repository, `Directory.Build.props`, `CLAUDE.md`
  plus guidance sync, ratchet baseline, `BACKLOG.md`, CI workflow with a pinned
  SHA, package floats, `.gitignore`, solution. Twelve games assembled from a
  checklist is twelve chances to drift.
- **[Entry 6, release notification](#6-widen-the-pre-10-version-float-from-0minor-to-0).**
  One enforcement release meant three manual boundary crossings on 2026-07-29;
  at twelve games that is twelve, per release. Adding packages without solving
  this makes the cadence worse, not better.

#### Open questions

- **Does `B44.Common`'s repository go private?** Recommendation: no. It ships
  `EmbedAllSources` and `DebugType=embedded`, so the full source is inside the
  `.nupkg` regardless — a private repo would hide the history and issues while
  publishing identical source. The choice is really public-source or
  private-package-plus-credentials, with nothing in between. Nothing in the
  repository is sensitive, and its decision records are the second-strongest
  portfolio artifact after `B44.Standards`.
- **How does `B44.Standards` split out?** Agreed in principle. It currently
  shares a repository and a release tag with `B44.Common`; separating them costs
  a repository, a trusted-publishing policy, a `NUGET_USER` secret, a CI pin,
  and its own publish cycle.
- **Do any packages need to be genuinely private?** If so, GitHub Packages is
  the free option — and knowingly re-adopts the PAT removed in `ee6f9ae`,
  which moved to nuget.org precisely because GitHub Packages' NuGet feed
  authenticates even public reads.
