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

Opt-in flags:

- `<B44Deterministic>true</B44Deterministic>` — bans ambient time/randomness
  (`DateTime.Now`, `new Random()`, …); inject `TimeProvider` / an explicit random
  source instead.
- `<B44EngineFreeCore>true</B44EngineFreeCore>` — additionally bans Godot APIs and
  fails the build if a Godot assembly reaches the resolved reference graph
  (implies determinism).

Unlicensed (all rights reserved).
