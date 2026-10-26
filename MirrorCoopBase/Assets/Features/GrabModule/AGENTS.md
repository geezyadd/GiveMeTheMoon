# GrabModule — interaction

Aim, grab, use and hold-to-use interaction. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
`Features.GrabModule` (layer 3; references: Camera, CharacterMovable, Contracts, NetworkModel).

## Key places
| What | Where |
|---|---|
| Interaction | `Assets/Features/GrabModule/`: `LocalPlayerInteraction` (aim, highlight, button; press and release for hold targets), `GrabController` (hand, `PlayerHand` model), `UseController` (single use and the server-run hold to use, `Hold/`), `HeldItemRegistry`, `InteractableBase`, `Grabbable`, `GrabConfiguration`. |
| Held controls | `Held/`: an interactable that also implements `IHeldControl` (server) and `IHeldControlDriver` (the holder's peer) is moved while the interact button is held, e.g. the ship's throttle lever. Press E on it: `LocalPlayerInteraction` sends `UseController.CmdTakeHeldControl` (reach + `CanUse`, then `IHeldControl.ServerTryTakeHold`), locks the view (`CameraLookLockModel`) and hands the mouse's look input (`MoveHeldControl`, also the tests' entry) to `IHeldControlDriver.LocalDrive` every frame; the driver says when to send a position (`HeldControlFrame`), sent by `CmdMoveHeldControl` (reach checked) to `ServerReceivePosition`. It ends on release (with the last position), a control block (death, a window), the driver reporting the hold lost, the control being destroyed (it went with its map: no last position then), and on the server when the player leaves reach, may no longer use it or disconnects (`ServerLetGo`). The crosshair leaving a moving handle does not end it. A new held control is a new interactable, no branch here. `InteractableBase.ServerUse` is virtual with an empty body (was abstract), so a held control does not override it; every other interactable still overrides it. |

An item leaving a hand fires `HeldItemRegistry.OnItemReleased` on that peer (the flying ship's deck catches an item let
go in flight with it), and `Grabbable.ApplyLoosePhysics` is the one place that gives an item nobody holds its physics
(the server simulates it, a client's network body moves it); the release and the ship's deck use it.

Seats and the helm are in [ShipModule](../ShipModule/AGENTS.md).

## Tests
- `Features.GrabModule.Editor.Tests` (`HoldUseProgressTests`: the hold completes after its time, resets on release, walking away, another item in the hand or an unusable target)
