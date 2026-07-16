# B44.Common

Engine-free shared primitives for B44 games, published as a private NuGet
package on GitHub Packages, plus the canonical B44-wide standards under
[`/templates`](templates/README.md).

## What's in the package (v0.1)

| Namespace | Types | Origin |
|---|---|---|
| `B44.Common` | `Vec2`, `Rgba`, `NumberFormatter`, `SafeConvert` | GameB / GameC / GameA |
| `B44.Common.Diagnostics` | `StructuredGameLogger`, `LogCategory` (name struct — games declare their own constants), `LogSeverity`, `LogVerbosityConfig`, `StructuredLogEvent` | merge of all three games |
| `B44.Common.Interfaces` | `IRandomSource` (+ default-interface `NextInt`/`NextDouble`), `SystemRandomSource`, `ITimeSource`, `SystemTimeSource`, `FakeTimeSource` | merge of GameB + GameC |
| `B44.Common.Persistence` | `IRepository<T>`, `AtomicJsonFileStore<T>`, `InMemoryRepository<T>`, `RepositoryFactory.CreateWithFallback`, `StoreException` | genericized from GameC |

Deliberately NOT here: `*ActionResult` shapes, content catalogs, game rules or
tuning, anything Godot-side. Sources and PDB are embedded in the assembly.

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
