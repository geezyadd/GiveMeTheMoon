# CrewInviteModule — inviting friends ("Hire crew")

The "Hire crew" terminal. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
- `Features.CrewInviteModule.Data` (layer 1; references: NetworkModel): the generated `CrewRoster` model.
- `Features.CrewInviteModule` (layer 6; references: CrewInviteModule.Data, Tooltip, Grab, Connection, NetworkModel): the "Hire crew" terminal; above Tooltip because it plugs a state tooltip into it through `ITooltipStateProvider`.

## Key places
| What | Where |
|---|---|
| Inviting friends ("Hire crew") | `Assets/Features/CrewInviteModule/` (`Scripts/Core`: rule and status, `Scripts/Network`: terminal, inviter, roster bridge, `Scripts/UI`: tooltip, `Data`: the generated model): the terminal `CrewInviteInteractable` stands in the lobby and on every `StationPad` next to the kiosk; E sends `CrewInviter.TargetOpenInviteOverlay` to the pressing player, who opens `ISteamLobbyService.OpenInviteOverlay` (Steam invite dialog for the current lobby). `CrewInviteRule` (pure) gives `Available` / `CrewFull` / `NotAtStation` / `NoSteam` / `OverlayDisabled` from the Steam side and the shared `CrewRoster` model (players, slots, join window from `IConnectionJoinPolicy`, written by the `CrewRoster` bridge on the server on `IConnectionJoinPolicy.OnJoinStateChanged`); `CrewInviteTooltipProvider` picks the `Tooltip_CrewInvite*` asset, fills "N/M" and refreshes on the roster and on `ISteamLobbyService.InviteStateChanged` |

## Tests
- `Features.CrewInviteModule.Editor.Tests`
