# B44.Common Backlog

Agreed work that has not started or has not yet shipped. Settled design rules and decision records live in [`CLAUDE.md`](CLAUDE.md). Completed work is removed after its next published release.

## Planned work

No B44.Common-local planned work is currently queued.

## Cross-repository programs

### Portfolio persistence framework

**Status:** **Deferred** since 2026-07-29.

Do not build migration infrastructure ahead of an actual compatibility promise. Revisit when a project approaches a stable release and two consumers need materially equivalent envelope behavior.

The current boundaries remain:

- `AtomicJsonFileStore` stays custom JSON-on-disk.
- Consumers own payload schemas, validation, migrations, supported historical versions, and destructive-data policy.
- Shared code may own durability, previous-good recovery, classified load results, ordered migration mechanics, and compatibility-test support once the trigger is met.
- `RepositoryFactory.CreateWithFallback` continues to require an explicit `UnreadableSavePolicy`.

### Scale shared packages from demonstrated use

**Status:** **Deferred** until a second-consumer extraction creates a concrete shared boundary.

Keep reusable game systems behind repository and package boundaries that reflect demonstrated use. Package count alone does not justify a new abstraction; promote a component only when independent consumers need materially equivalent behavior.

## Known defects

No known defects are currently queued in this repository.
