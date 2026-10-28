# StagePresetModule — playtest stage presets

The playtest stage presets. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
- `Features.StagePresetModule` (layer 6; references: StagePresetModule.Data, Ship, Shop, FlightField, Mvp, Connection, NetworkModel): the playtest stage presets.
- `Features.StagePresetModule.Data` (layer 1): its synced model.

## Playtest stage presets
To test mid- and late-game fun without flying 15 loops, the host picks a stage preset in the lobby before Start: the
"Stage preset (playtest)" panel under Start offers Normal run, Early (loop 0, two propellers, 300), Mid (loop 4, two
large engines, the radar and the minigun, 1 500) and Late (loop 9, the same modules, 2 400); presets give no free station drops, and neither does the normal run (`StagePresetCatalog._areNormalRunStationDropsEnabled`, off since 2026-10-26: the crew starts with 300 and buys its modules).
Everyone sees "Preset: <name>" under the balance; the choice ends with the run. The panel shows only in editor and
development builds (release: `StagePresetCatalog._isPickerShownInReleaseBuilds`). A new preset is a new `StagePreset`
asset in `StagePresetCatalog_Default`: modules from the `ModuleCatalog`, socket ids of `ShipPlatform` (`engine-left`,
`engine-right`, `radar`, `control`, `turret-front`). A system that needs its own preset setting (hull HP, ore drops, ...) puts a
ScriptableObject block into a preset's extensions and reads it with
`IStagePresetSelectionService.TryGetSelectedExtension<T>`; the preset core does not know the block. No preset has a
block now: the balloon count block (`FieldTargetPresetBlock`) went with the target balloons on 2026-10-28.

## Key places
| What | Where |
|---|---|
| Stage presets (playtests) | `Assets/Features/StagePresetModule/`: a preset is one `StagePreset` asset (`GameResources/Presets/Early`, `Mid`, `Late`: id, name, description, start loop, optional field profile id, module per socket id, starting balance, station drops on / off, optional extension blocks) in `StagePresetCatalog_Default` (`Configurations` group; `StagePresetCatalogValidator` checks it at startup, the sockets are checked against the ship at the run start by `StagePresetSocketResolver`). The host's choice is the server-side `StagePresetSelectionModel`; `StagePresetSync` (a scene object in LobbyScene and GameScene) carries it to every client and late joiner in the synced `RunStagePreset` model. `StagePresetApplier` is the one server place that turns it into the run: `IShipRunStage` (Ship: start loop and drops read by `ShipRunService.Bind` / `ShipStationDropService`, modules installed through `ShipSocket.ServerTryInstall` after `ServerResetForBuild`, the field profile through `IFieldProfileDebug`) and `IWalletStartingBalanceSource` (Shop: `CrewWallet`'s starting balance). The run's end (`ShipRunService` reset: back to the lobby or the menu) clears the choice. UI in `GameHudWindow`: `StagePresetPickerView` (host, lobby only, editor / development builds or `StagePresetCatalog._isPickerShownInReleaseBuilds`) and `StagePresetLabelView` ("Preset: Late" for everyone) |

## Tests
- `Features.StagePresetModule.Editor.Tests` (shipped presets pass the startup validator and fit ShipPlatform's sockets, Early / Mid / Late loops and money against shop prices, broken presets throw, the applier's run values)
