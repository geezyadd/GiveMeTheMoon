# MatrixModule — grid and path finding

A generic grid. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
`Features.MatrixModule` (layer 1; no references): generic grid `IMatrix<T>` with shape insert and A* path finding, copied from MuffinProject's `MatrixSubmodule`; keep its public API.

## Tests
- `Features.MatrixModule.Editor.Tests` (matrix creation, neighbours, full-shape insert incl. L-shaped shapes, path finding incl. a 40x40 timing check)
