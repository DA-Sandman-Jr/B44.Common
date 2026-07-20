# B44 repository bootstrap templates

Starting points for repository-owned configuration. Ongoing analyzer and agent
policy is versioned in `B44.Standards`; these files should not become copied
policy forks.

| File | Goes to | Notes |
|---|---|---|
| `Directory.Build.props` | repo root | Select the `Game` guidance profile, repository-relative sync anchor, mandatory engine-free Core project, and pinned B44.Standards version |
| `format.yml` | `.github/workflows/` | dotnet-format gate |
| `build-test.yml` | `.github/workflows/` | Build + test gate; replace `GAME` placeholders; B44 packages restore directly from nuget.org |
| `nuget.config` | repo root | Optional deterministic nuget.org-only package source; no credentials |
| `CLAUDE.skeleton.md` | new root `CLAUDE.md` | Repository-local starter only; B44.Standards inserts and maintains the canonical managed sections |
| `TestProject.godot-guard.snippet.xml` | test csproj | MSBuild error if a Godot reference sneaks into the test/Core chain |
