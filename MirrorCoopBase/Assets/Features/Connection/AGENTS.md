# Connection — joining a running session

Connection, authentication and the join rule. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Key places
| What | Where |
|---|---|
| Joining a running session | `Connection/Scripts/Game.Connection/Join/`: `JoinRule` (pure: lobby or `RunJoinWindow.AtStation`, free slot out of `ConnectionConfig.MaxConnections`, version, Steam id; reasons `InFlight` / `ServerFull` / `MapChanging`), `ConnectionJoinPolicy` (the rule on the live session, used by `ConnectionAuthenticator` once at authentication; `OnJoinStateChanged` is the one event for "an input of the rule changed": run window, map change, players), `SteamLobbyJoinableSystem` (Steam lobby joinable exactly when the rule lets a new player in), `ConnectionRejectModel` + `ConnectionRejectMessages` (the reason shown in the menu; `RejectedJoinMenuSystem` in GameCore switches a turned-away client to the menu state). Connection reads the run only through the contracts `ISessionJoinGate` (`ShipRunJoinGate`) and `ISessionJoinSpawn` (`ShipDeckJoinSpawn`, the next `IShipDeckSpawnPoints` pose) in `GameCoreModule.Contracts`. A mid-run joiner loads the map through Mirror's scene message and reports `MapLoadedMessage`; `ConnectionLateJoin` makes its player only then, on the deck. The transport allows one connection over the slots so an extra player hears "the crew is full" |

## Tests
- `Game.Connection.Editor.Tests`
