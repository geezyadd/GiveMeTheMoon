# GrabModule — interaction

Aim, grab, use and hold-to-use interaction. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
`Features.GrabModule` (layer 3; references: Camera, CharacterMovable, Contracts, NetworkModel).

## Key places
| What | Where |
|---|---|
| Interaction | `Assets/Features/GrabModule/`: `LocalPlayerInteraction` (aim, highlight, button; press and release for hold targets), `GrabController` (hand, `PlayerHand` model), `UseController` (single use and the server-run hold to use, `Hold/`), `HeldItemRegistry`, `InteractableBase`, `Grabbable`, `GrabConfiguration`. |

Seats and the helm are in [ShipModule](../ShipModule/AGENTS.md).

## Tests
- `Features.GrabModule.Editor.Tests` (`HoldUseProgressTests`: the hold completes after its time, resets on release, walking away, another item in the hand or an unusable target)
