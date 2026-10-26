# PlayerProfileModule — player names and nameplates

Player names and nameplates. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
- `Features.PlayerProfileModule` (layer 4; references: PlayerProfileModule.Data, Camera, NetworkModel, TextMeshPro).
- `Features.PlayerProfileModule.Data` (layer 1; references: NetworkModel): the generated `PlayerProfile` model and `IPlayerProfileSource`.

## Key places
| What | Where |
|---|---|
| Player names and nameplates | `Assets/Features/PlayerProfileModule/`: `PlayerProfile` (bridge of the `PlayerProfile` model, the server fills the name), `PlayerNameSanitizer`, `PlayerNameplate` (TMP over other players' heads), `PlayerNameplateConfiguration`; the model is in `Data/` |

## Tests
- `Features.PlayerProfileModule.Editor.Tests`
