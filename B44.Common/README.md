# B44.Common

Engine-free shared primitives from the B44 game studio. No Godot or other engine
dependency — usable in any .NET 8 project.

- **`Diagnostics.StructuredGameLogger`** — sink-delegate structured logging with
  per-category verbosity and correlation scopes.
- **`Interfaces.IRandomSource` / `SystemRandomSource`** — injectable, seedable
  randomness so domain logic stays deterministic under test.
- **`Persistence.AtomicJsonFileStore<T>`** — durable JSON store: flush-to-disk
  before an atomic rename, `.bak` rotation with automatic recovery on load.
  Ships with `IRepository<T>`, `InMemoryRepository<T>`, a fallback factory, and
  `SavePaths`.
- **`Quality.SourceSizeRatchet`** — baseline-pinned file-size check you run from
  your own test suite (new files capped; baselined files may shrink, never grow).
Unlicensed studio tooling (all rights reserved), published for our own
credential-free restore across repos.
