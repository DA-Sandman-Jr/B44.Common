# B44.Common

Engine-free .NET primitives shared across B44 Labs games.

- **Structured diagnostics** — category-based verbosity, structured fields,
  correlation scopes, and pluggable sinks.
- **Deterministic randomness** — injectable and seedable through
  `IRandomSource`, with sequences pinned to `System.Random` behavior.
- **Durable JSON persistence** — flush-before-replace atomic writes, last-good
  backup rotation and recovery, repository abstractions, explicit unreadable
  save policy, and classified store failures.

The package contains reusable mechanisms only. Game rules, content, tuning,
save schemas, and engine integration remain in their owning repositories.

See the [source repository](https://github.com/DA-Sandman-Jr/B44.Common) for
design records, usage guidance, and the complete test suite.

Copyright (c) 2026 David Sanders / B44 Labs. All rights reserved.
