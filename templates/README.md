# B44 canonical templates

The one true copy of every cross-repo standard. Game repos copy these files
and must not fork them — fix drift by updating the template here first, then
re-copying.

| File | Goes to | Notes |
|---|---|---|
| `Directory.Build.props` | repo root | Set `B44GameProjectName` to the root game csproj name — the only repo-specific line |
| `Directory.Build.targets` | repo root | Byte-identical everywhere; AGENTS.md sync driven by `B44GameProjectName` |
| `format.yml` | `.github/workflows/` | dotnet-format gate |
| `build-test.yml` | `.github/workflows/` | Build + test gate; replace `GAME` placeholders; needs the `B44_PACKAGES_PAT` repo secret once the game consumes B44.Common |
| `nuget.config` | repo root | Adds the B44 package source; credentials never go here |
| `CLAUDE.skeleton.md` | root `CLAUDE.md` (top sections) | Studio doctrine: hard rules, conventions, Architecture Ratchet, Sunset Guarantee |
| `TestProject.godot-guard.snippet.xml` | test csproj | MSBuild error if a Godot reference sneaks into the test/Core chain |
