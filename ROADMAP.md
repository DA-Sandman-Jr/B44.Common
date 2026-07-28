# B44.Common / B44.Standards Roadmap

Planned work that is agreed in principle but not yet scheduled into a release.
Decisions that are already settled live in [`CLAUDE.md`](CLAUDE.md) (hard rules
and decision records) — this file is for what is still ahead.

Status values: **Planned** (agreed, not started), **In progress**, **Blocked**,
**Done** (drop the entry at the next release and record it in `CLAUDE.md` or
the READMEs if it changed a rule).

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

### 1. Add the small `B44.Godot` package; migrate the logger sinks and `NodePathValidator`

**Status:** Planned

Each game currently keeps its own Godot-side adapter code, which is the second
occurrence several times over:

- `GodotLoggerFactory` — the per-game factory that builds the sink delegate
  `StructuredGameLogger` takes (see
  [`StructuredGameLogger.cs:73`](B44.Common/Diagnostics/StructuredGameLogger.cs:73)).
- The `GD.PushWarning` warning sink passed to
  `RepositoryFactory.CreateWithFallback` (see
  [`RepositoryFactory.cs:7`](B44.Common/Persistence/RepositoryFactory.cs:7)).
- `NodePathValidator`, alongside the `*Paths.cs` convention.

Scope it deliberately small: a thin adapter package over primitives that already
exist in `B44.Common`, not a second home for game logic. The second-occurrence
rule applies here exactly as it does to `B44.Common`.

**Open questions to resolve before starting:**

- **Where does it live?** Category A of
  [Isolation boundaries](#isolation-boundaries--decide-once-applies-to-everything-after)
  above. Settle that first — it decides this, and no code moves before it does.
- **Godot version coupling.** Godot-side code churns with engine releases. Pin
  the supported Godot/GodotSharp range and decide whether `B44.Godot` versions
  independently of `B44.Common` (it probably must).
- **Standards profile.** `B44.Godot` cannot take `B44EngineFreeCore=true`.
  Confirm which analyzer layer it does take, and whether `B44Deterministic`
  still applies.

### 2. Convert the bootstrap snippets into a real B44 game template

**Status:** Planned

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
