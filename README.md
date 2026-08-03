# B44.Common

Engine-free .NET primitives shared across B44 Labs games. The package keeps
cross-game mechanics small, deterministic, and independently testable while
engine integration remains behind adapter boundaries.

## What it provides

| Area | Public surface |
|---|---|
| Diagnostics | Structured events, category-based verbosity, correlation scopes, and pluggable sinks |
| Randomness | `IRandomSource` plus a seeded `SystemRandomSource` whose sequence is pinned to `System.Random` |
| Persistence | Repository abstractions, atomic JSON writes, last-good backup recovery, save paths, and classified store failures |
| Recovery policy | An explicit `UnreadableSavePolicy` at the composition boundary so destructive behavior is visible at the call site |

## Design guarantees

- No Godot or other engine references.
- Shared mechanisms only; game rules, content, tuning, and save schemas stay in
  their owning games.
- Seeded randomness is treated as a compatibility surface and covered by
  sequence-pinning tests.
- Persistence flushes data before atomic replacement and automatically tries
  the previous-good backup before reporting unreadable storage.
- New primitives must have a demonstrated second consumer.

## Consuming the package

Use compatibility-bounded floats for B44 packages while they are pre-1.0:

```xml
<ItemGroup>
  <PackageReference Include="B44.Common" Version="0.11.*" />
  <PackageReference Include="B44.Standards" Version="0.10.*" PrivateAssets="all" />
</ItemGroup>
```

`B44.Standards` owns build policy, analyzer configuration, source-size gates,
agent-guidance synchronization, reusable CI, and repository templates. Its
source and documentation live in the
[`B44.Standards`](https://github.com/DA-Sandman-Jr/B44.Standards) repository.
Godot-specific adapters live separately in
[`B44.Godot`](https://github.com/DA-Sandman-Jr/B44.Godot).

## Build and test

```bash
dotnet restore B44.Common.sln
dotnet build B44.Common.sln --no-restore
dotnet test B44.Common.sln --no-build
```

The test suite covers deterministic sequences, structured logging, repository
behavior, atomic replacement, backup recovery, and failure policy.

## Versioning and publishing

The package remains `0.x.y` while its API evolves; breaking changes increment
the minor version. Consumers cross that compatibility boundary deliberately,
while compatible patches flow through the bounded float.

Pushing a `v*` tag runs the release workflow, tests the repository, packs only
`B44.Common`, and publishes through NuGet Trusted Publishing without a
long-lived API key.

## License

Copyright (c) 2026 David Sanders / B44 Labs. All rights reserved. The source is
publicly available for review but is not licensed for reuse.
