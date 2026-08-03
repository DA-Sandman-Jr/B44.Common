# B44.Common Backlog

Agreed work that has not started or has not yet shipped. Settled design rules
and decision records live in [`CLAUDE.md`](CLAUDE.md). Completed work is removed
after its next published release.

Status values: **Planned**, **In progress**, **Blocked**, and **Deferred**.

---

## Planned Work

### 1. Remove the deprecated `SourceSizeRatchet` API

**Status:** **In progress for 0.11.0.** The build-time
`B44VerifyRatchet` / `B44WriteRatchetBaseline` targets in `B44.Standards`
replaced `B44.Common.Quality.SourceSizeRatchet` in 0.8.1. All current games use
the build target and have no remaining source reference to the deprecated type.

The type and its compatibility tests are removed together in the 0.11.0
candidate. Measure that candidate against every consumer before publishing,
then advance each `B44.Common` compatibility boundary deliberately.

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

**Status:** **In progress.** `B44.Godot` is independently published;
`B44.Standards` plus `B44.Templates` moved to their own public repository and
published successfully as 0.10.1; and every reusable-CI consumer now calls the
reviewed workflow in `B44.Standards`.

Remaining sequence:

1. Create `B44.Games` only when a concrete second-game extraction is ready;
   package count alone does not justify a repository.
2. Extract shared inventory and dungeon mechanics only after stable lifecycle
   seams exist in their current consumers.

### External: B44.Godot shared adapters

**Status:** **Planned in B44.Godot.** Composition smoke testing is complete and
used by two games. Further adapter migration is tracked in
[B44.Godot's backlog](https://github.com/DA-Sandman-Jr/B44.Godot/blob/main/BACKLOG.md).

---

## Known Defects

No known defects are currently queued in this repository.
