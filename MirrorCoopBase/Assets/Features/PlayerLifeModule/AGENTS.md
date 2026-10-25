# PlayerLifeModule — death, damage, spectator

Player life, damage, death and the spectator. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
`Features.PlayerLifeModule` (layer 5; references: Ship, Grab, Camera, Mvp, NetworkModel, Connection, PlayerProfileModule.Data).

## Key places
| What | Where |
|---|---|
| Death, damage, spectator | `Assets/Features/PlayerLifeModule/`: `PlayerLifeSystem` (life state machines, all dead → lobby), `PlayerDamageable`, `FallKillSystem`, `SpectatorSystem`, `PlayerLifeConfiguration`, `PlayerDamageConfiguration`; the debug window's Player → "Kill me" (`DebugLocalPlayerKillSystem`) kills the local player in editor / development builds |

## Tests
- `Features.PlayerLifeModule.Editor.Tests`
