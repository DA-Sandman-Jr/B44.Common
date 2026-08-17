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
   `B44.GameSystems` fills that role as the home for B44-owned reusable game
   systems whether they arrive from planning or extraction. A second repository
   for the same role would be indistinguishable from it. The original
   second-consumer gating rule still applies to what lands there — package count
   alone does not justify a boundary.

---

## Known Defects

No known defects are currently queued in this repository.
