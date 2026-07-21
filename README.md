# B44.Common

Engine-free shared primitives and studio policy for B44 repositories, published
as the `B44.Common` and `B44.Standards` packages on nuget.org. Bootstrap
examples remain under [`/templates`](templates/README.md); ongoing build and
agent policy ships through `B44.Standards`.

## What's in B44.Common

| Namespace | Types | Origin |
|---|---|---|
| `B44.Common.Diagnostics` | `StructuredGameLogger`, `LogCategory` (name struct — games declare their own constants), `LogSeverity`, `LogVerbosityConfig`, `StructuredLogEvent` | merge of all three games |
| `B44.Common.Interfaces` | `IRandomSource` (+ default-interface `NextInt`/`NextDouble`), `SystemRandomSource` | merged from the games |
| `B44.Common.Persistence` | `IRepository<T>`, `AtomicJsonFileStore<T>`, `InMemoryRepository<T>`, `RepositoryFactory.CreateWithFallback`, `SavePaths`, `StoreException` | genericized from one game's store |
| `B44.Common.Quality` | `SourceSizeRatchet` (baseline-pinned file-size check each repo runs from its test suite) | new — mechanizes the Architecture Ratchet |

## B44.Standards — build policy as a package

The sibling `B44.Standards` package carries the studio's enforcement stack and
applies it to any referencing project via buildTransitive assets: SDK analyzers
at `AnalysisMode=Recommended`, an opt-in target-level-pinned full Security
profile, a curated Meziantou allowlist + banned-API rules
(severities in `config/B44.globalconfig`, with a `*.Tests` overlay),
`CA1502`/`MA0051` complexity+length thresholds, NuGet audit on restore, and —
for projects that set `<B44EngineFreeCore>true</B44EngineFreeCore>` — the
banned-symbols list (no Godot APIs, no ambient time/randomness) plus an MSBuild
guard that fails the build if a Godot assembly appears in the resolved
reference graph. Its opt-in agent target also maintains canonical organization
and game guidance in root `CLAUDE.md` files and recursively generates sibling
`AGENTS.md` mirrors. Consume with:

```xml
<PackageReference Include="B44.Standards" Version="0.6.*" PrivateAssets="all" />
```

This repo dogfoods the same files through its root `Directory.Build.props` and
`Directory.Build.targets`. CI for game repos calls
`.github/workflows/reusable-dotnet-ci.yml` (`workflow_call`; repository Actions
access is set to same-owner).

Deliberately NOT here: `*ActionResult` shapes, content catalogs, game rules or
tuning, anything Godot-side. Sources and PDB are embedded in the assembly.

Deliberately replaced by the BCL instead of shipping our own (v0.2):

- **2D vectors** — use `System.Numerics.Vector2` (each game keeps its one
  Godot bridge file). The old custom `Vec2` is gone.
- **Time** — inject the BCL `TimeProvider` (`TimeProvider.System` in
  production, `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`
  in tests) and call `GetUtcNow().ToUnixTimeSeconds()` where needed. The old
  `ITimeSource` family is gone.

Evicted in v0.3 for failing the second-occurrence rule (both were
single-consumer): `NumberFormatter` (lives in the one game that uses it) and `SafeConvert` (deleted outright — it had zero call sites even in its origin game). Re-admit either the moment a second game actually needs it.

Evicted in v0.5 for the same reason: `Rgba` returned to TicTacHoe after
Whispers' byte-based `GameColor` proved intentionally incompatible, and
`TimeProviderExtensions.GetUtcNowUnixSeconds()` returned to TimeMachineClicker.
Their focused tests moved with them. Re-admit either only when a second
repository needs the exact same representation or behavior.

## Consuming (game repos)

1. Reference the required package versions directly from nuget.org:
   ```xml
   <PackageReference Include="B44.Common" Version="0.5.*" />
   <PackageReference Include="B44.Standards" Version="0.6.*" PrivateAssets="all" />
   ```
2. Opt into synchronized agent guidance from the repository's
   `Directory.Build.props` if it is a B44-owned repository; see
   [`B44.Standards/README.md`](B44.Standards/README.md).

### Iterating on the package from a game repo

Swap the `PackageReference` for a local `ProjectReference` to the sibling
clone while developing, or pack locally into a folder feed. Publish a tag and
restore the `PackageReference` before merging.

## License

None — all rights reserved. The source is public for reference; it is not
licensed for reuse.

## Publishing

Push a version tag: `git tag v0.6.0 && git push origin v0.6.0`. The publish
workflow tests and packs both packages, then publishes them to nuget.org via
Trusted Publishing (OIDC; no long-lived API key).
