# B44.Common

Engine-free shared primitives for B44 games, published as a private NuGet
package on GitHub Packages, plus the canonical B44-wide standards under
[`/templates`](templates/README.md).

## What's in the package (v0.1)

| Namespace | Types | Origin |
|---|---|---|
| `B44.Common` | `Rgba`, `TimeProviderExtensions` | GameB / all games |
| `B44.Common.Diagnostics` | `StructuredGameLogger`, `LogCategory` (name struct — games declare their own constants), `LogSeverity`, `LogVerbosityConfig`, `StructuredLogEvent` | merge of all three games |
| `B44.Common.Interfaces` | `IRandomSource` (+ default-interface `NextInt`/`NextDouble`), `SystemRandomSource` | merge of GameB + GameC |
| `B44.Common.Persistence` | `IRepository<T>`, `AtomicJsonFileStore<T>`, `InMemoryRepository<T>`, `RepositoryFactory.CreateWithFallback`, `SavePaths`, `StoreException` | genericized from GameC |
| `B44.Common.Quality` | `SourceSizeRatchet` (baseline-pinned file-size check each repo runs from its test suite) | new — mechanizes the Architecture Ratchet |

## B44.Standards — build policy as a package

The sibling `B44.Standards` package carries the studio's enforcement stack and
applies it to any referencing project via buildTransitive assets: SDK analyzers
at `AnalysisMode=Recommended`, a curated Meziantou allowlist + banned-API rules
(severities in `config/B44.globalconfig`, with a `*.Tests` overlay),
`CA1502`/`MA0051` complexity+length thresholds, NuGet audit on restore, and —
for projects that set `<B44EngineFreeCore>true</B44EngineFreeCore>` — the
banned-symbols list (no Godot APIs, no ambient time/randomness) plus an MSBuild
guard that fails the build if a Godot assembly appears in the resolved
reference graph. Consume with:

```xml
<PackageReference Include="B44.Standards" Version="x.y.z" PrivateAssets="all" />
```

This repo dogfoods the same files via `Directory.Build.props`. CI for game
repos: call `.github/workflows/reusable-dotnet-ci.yml` (workflow_call; repo
Actions access is set to same-owner).

Deliberately NOT here: `*ActionResult` shapes, content catalogs, game rules or
tuning, anything Godot-side. Sources and PDB are embedded in the assembly.

Deliberately replaced by the BCL instead of shipping our own (v0.2):

- **2D vectors** — use `System.Numerics.Vector2` (each game keeps its one
  Godot bridge file). The old custom `Vec2` is gone.
- **Time** — inject the BCL `TimeProvider` (`TimeProvider.System` in
  production, `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`
  in tests). `TimeProviderExtensions.GetUtcNowUnixSeconds()` keeps unix-second
  call sites clean. The old `ITimeSource` family is gone.

Evicted in v0.3 for failing the second-occurrence rule (both were
single-consumer): `NumberFormatter` (lives in GameC, its only
user) and `SafeConvert` (deleted outright — it had zero call sites even in
GameA). Re-admit either the moment a second game actually needs it.

## Consuming (game repos)

1. Copy `templates/nuget.config` to the repo root.
2. **Local dev (once per machine):** create a classic PAT with `read:packages`, then
   ```bash
   dotnet nuget update source b44 --username DA-Sandman-Jr \
     --password YOUR_PAT --store-password-in-clear-text
   ```
   (writes to the user-level NuGet config, never the repo).
3. **CI:** add the PAT as the `B44_PACKAGES_PAT` repo secret; the templated
   `build-test.yml` registers the source before restore. Another repo's
   `GITHUB_TOKEN` cannot read a user-owned private package — this secret is
   required.
4. Reference it:
   ```xml
   <PackageReference Include="B44.Common" Version="0.1.0" />
   ```

### Iterating on the package from a game repo

Swap the `PackageReference` for a local `ProjectReference` to the sibling
clone while developing, or pack locally into a folder feed. Publish a tag and
restore the `PackageReference` before merging.

## Publishing

Push a version tag: `git tag v0.2.0 && git push origin v0.2.0`. The publish
workflow tests, packs with that version, and pushes to the
`DA-Sandman-Jr` GitHub Packages NuGet feed using the repo's own
`GITHUB_TOKEN`.
