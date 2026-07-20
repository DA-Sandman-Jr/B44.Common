# B44.Standards

B44 studio build policy as a package. **Studio-internal tooling** — published so
our own repos can restore it without credentials; it encodes B44 conventions and
isn't a general-purpose library.

Reference it with `PrivateAssets="all"` and it applies, via buildTransitive
assets:

- SDK analyzers at `AnalysisMode=Recommended`, a curated Meziantou allowlist, and
  banned-API rules — severities in a packaged global analyzer config.
- `CA1502` / `MA0051` complexity and method-length thresholds.
- NuGet vulnerability audit on restore.
- Opt-in synchronization of canonical organization/game guidance into marked
  root `CLAUDE.md` sections, plus recursive sibling `AGENTS.md` generation.

Opt-in flags:

- `<B44Deterministic>true</B44Deterministic>` — bans ambient time/randomness
  (`DateTime.Now`, `new Random()`, …); inject `TimeProvider` / an explicit random
  source instead.
- `<B44EngineFreeCore>true</B44EngineFreeCore>` — additionally bans Godot APIs and
  fails the build if a Godot assembly reaches the resolved reference graph
  (implies determinism).

Agent guidance synchronization is off unless a repository opts in from its
root `Directory.Build.props`:

```xml
<B44AgentSyncEnabled>true</B44AgentSyncEnabled>
<B44AgentGuidanceProfile>Organization</B44AgentGuidanceProfile>
<B44AgentRepositoryRoot>$(MSBuildThisFileDirectory)</B44AgentRepositoryRoot>
<B44AgentSyncProject>$(MSBuildThisFileDirectory)src\App\App.csproj</B44AgentSyncProject>
```

Use `Game` instead of `Organization` to add the game rules. Set
`B44GameCoreProject` to the mandatory engine-free `*.Core.csproj`, make that
same project the synchronization anchor, and set `B44EngineFreeCore=true` in
its project file. The anchor makes synchronization run once; all paths remain
repository-relative.
Local builds update managed files, while
`-p:B44AgentSyncVerifyOnly=true` validates them without writing.

Active, unreleased repositories may reference `B44.Standards` with a bounded
patch float such as `0.4.*`. Released or production repositories pin an exact
Standards version, and all `B44.Common` runtime references stay exact. Changes
that expand enforcement or otherwise require coordinated adoption bump the
Standards minor version rather than entering an existing patch float.

Synchronization does not traverse common dependency, build-output, coverage,
publish, IDE, or virtual-environment directories, and it never follows directory
reparse points. Repositories can add their own generated or imported subtrees;
paths are interpreted relative to `B44AgentRepositoryRoot` and must remain
inside it:

```xml
<ItemGroup>
  <B44AgentSyncExclude Include="generated-site" />
  <B44AgentSyncExclude Include="vendor/imported-project" />
</ItemGroup>
```

Unlicensed (all rights reserved).
