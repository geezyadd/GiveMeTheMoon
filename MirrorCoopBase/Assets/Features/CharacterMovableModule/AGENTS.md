# CharacterMovableModule — player movement

The player's walking body at a station, its stats and the drift in open space. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file. Riding the flying deck is the ship's ([ShipModule](../ShipModule/AGENTS.md), "Riding the ship").

## Assembly
`Features.CharacterMovableModule` (layer 2; references: Contracts, FloatingController, Address, Stats, Input, Mirror, Zenject).

## Key places
| What | Where |
|---|---|
| Walking body | `CharacterMovableBase` (owner simulates the dynamic body: walk, jump, air gravity; the server shoves items for a client-owned body), `JumpAndAirGravity` / `JumpTuning` (shared with the ship ride), `CharacterInputBuffer`, stats in `PlayerStats/` |
| Drift in open space | `Drift/`: `CharacterDrift` (player prefab, every peer) turns gravity and the hover off on the owner's peer, keeps the body's velocity and steps it with `CharacterDriftMotion` (the speed above the slow drift fades exponentially, a very weak push of the move keys along the view up to its own cap, an outside push such as a shot's recoil, or a rope's exact pull velocity); every peer tumbles the `Visual` slowly round the body's middle and swings four limbs (`CharacterDriftTumble`, local, nothing synced). While it drifts `CharacterMovableBase` does not simulate. The numbers (`CharacterDriftSettings`) live in the ship's `ShipOverboardConfiguration`; who starts and ends a drift is the ship's overboard rule ("Overboard" in [ShipModule](../ShipModule/AGENTS.md)) |

## Tests
- `Features.CharacterMovableModule.Editor.Tests`: `PlayerStatDefaultsTests`, `CharacterDriftMotionTests` (a fast exit fades to the drift speed along its direction, a slow drift keeps its speed, no force keeps the height, the push stays under its cap and gives only a weak nudge).
