# B44.Common Backlog

Agreed work that has not started or has not yet shipped. Settled design rules and decision records live in [`CLAUDE.md`](CLAUDE.md). Completed work is removed after its next published release.

## Planned work

No B44.Common-local planned work is currently queued.

## Cross-repository programs

### Portfolio persistence framework

**Status:** **Deferred** since 2026-07-29.

Do not build migration infrastructure ahead of an actual compatibility promise. This is generalized infrastructure, not a bounded capability, so it keeps the higher bar: revisit when a project approaches a stable release and two consumers need materially equivalent envelope behavior.

The current boundaries remain:

- `AtomicJsonFileStore` stays custom JSON-on-disk.
- Consumers own payload schemas, validation, migrations, supported historical versions, and destructive-data policy.
- Shared code may own durability, previous-good recovery, classified load results, ordered migration mechanics, and compatibility-test support once the trigger is met.
- `RepositoryFactory.CreateWithFallback` continues to require an explicit `UnreadableSavePolicy`.

### Scale shared packages from demonstrated use

**Status:** **Standing constraint**, not scheduled work.

Keep reusable game systems behind repository and package boundaries that reflect demonstrated use. Package count alone does not justify a new abstraction.

Extraction is judged on the capability, not on a headcount of repositories. A bounded reusable capability may be extracted from a single real consumer when its seam is small and coherent, its API stays domain-facing, independent evidence says the reuse is real, and nothing speculative has to be built around it. A second consumer is one form of that evidence, not a precondition. Generalized infrastructure — cross-capability foundations, orchestration, registries and schedulers, authority or transaction frameworks — keeps the higher bar and normally needs at least two independent real consumers.

Choosing a home is a separate decision from recognizing a capability: shared behavior belongs to the package that naturally owns it, and nothing lands in `B44.Common` by default. A primitive that turns up independently in a second repository is an ownership-review trigger, recorded here with both call sites, not an automatic extraction.

## Known defects

No known defects are currently queued in this repository.
