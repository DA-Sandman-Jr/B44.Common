# B44.Common Backlog

Agreed work that has not started or has not yet shipped. Settled design rules
and decision records live in [`CLAUDE.md`](CLAUDE.md). Completed work is removed
after its next published release.

Status values: **Planned**, **In progress**, **Blocked**, and **Deferred**.

---

## Planned Work

No B44.Common-local planned work is currently queued.

---

## Cross-repository programs

### P1. Portfolio persistence framework

**Status:** **Deferred** since 2026-07-29. Do not build migration infrastructure
ahead of an actual compatibility promise. Start when a game approaches 1.0 and
two games need materially equivalent envelope behavior.

The existing decisions remain:

- `AtomicJsonFileStore` stays custom JSON-on-disk.
- Games own payload schemas, validation, migrations, supported historical
  versions, and destructive-data policy.
- Shared code may own durability, previous-good recovery, classified load
  results, ordered migration mechanics, and compatibility-test support once the
  trigger is met.
- `RepositoryFactory.CreateWithFallback` continues to require an explicit
  `UnreadableSavePolicy`.

Planned waves:

| Wave | Owning repository | Outcome |
|---|---|---|
| 1 | B44.Common | Minimal envelope boundaries, classified load results, ordered migration mechanics, and shared compatibility-test support |
| 2 | Whispers | Document and test one capture-and-quiescence contract; independent of Wave 1 |
| 3 | Whispers + B44.Common | Adopt the shared framework and preserve unsupported future data |
| 4 | Time Machine Clicker + TicTacHoe | Adopt the framework while preserving each game's canonical state ownership and explicit pre-release policy |
| 5 | All consumers | Add game-owned migrations and historical fixtures only where compatibility is promised |

### P2. Scale shared packages for a twelve-game portfolio

**Status:** **Deferred** until a second-game extraction creates a concrete
shared boundary.

The portfolio foundation is in place: `B44.Godot` is independently published;
`B44.Standards` and `B44.Templates` are maintained in their own public
repository; current games consume the shared engine adapters; and reusable-CI
consumers call the reviewed workflow in `B44.Standards`.

Remaining sequence:

1. The planned `B44.Games` repository is superseded as of 2026-08-07.
   `B44.GameSystems` fills that role: created to implement Epic 1 (see P3), it
   is the home for B44-owned reusable game systems whether they arrive from
   planning or from extraction. A second repository for the same role would be
   indistinguishable from it. The original gating rule still applies to what
   lands there — package count alone does not justify a boundary.
2. Extraction of shared inventory and dungeon mechanics moved to
   `B44.GameSystems`' backlog as G2, with its gating condition unchanged:
   extract only after stable lifecycle seams exist in their current consumers.

### P3. Shared causal foundation

**Status:** **In progress** since 2026-08-07.

An engine-independent causal foundation that lets consumer-defined domains
prepare, admit, coordinate, finalize, and publish authoritative operations
deterministically, without imposing a universal game model. Planned as a
multi-Epic program; only Epic 1 is scheduled.

`B44.Common` owns no implementation here. This entry exists only because the
program spans repositories.

| Epic | Owning repository | Relationship |
|---|---|---|
| 1 — Shared Causal Foundation | `B44.GameSystems` | Implementation; see its G1 |
| 2 and sibling domain Epics | Not scheduled | Consume Epic 1; Epic 1 never consumes them |
| 7 — Audience Projection | Not scheduled | Downstream observer; published immutable observations only |
| 8 — Determinism Diagnostics | Not scheduled | Optional observer/evidence seams; never authority |
| Consumer games | Their own repositories | Adopt after Epic 1 acceptance; never a foundation prerequisite |

Dependencies point inward throughout: no part of the program requires a change
to `B44.Standards`, `B44.Common`, or `B44.Godot`.

---

## Known Defects

No known defects are currently queued in this repository.
