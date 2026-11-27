# InventoryModule — the player's inventory slots

Four slots (count from the config) for hand weapons and tools, the selected slot is what the hand holds; keys 1–4, the
mouse wheel, Q to drop or throw, and the HUD bar. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules,
module map) and [GrabModule](../GrabModule/AGENTS.md) (the hand). Change this module's description here, not in the
root file.

## Assembly
`Features.InventoryModule` (layer 4; references: Grab, NetworkModel, CharacterMovable, Camera, Input, InputRealization,
Mvp, Mirror, Zenject, UnityEngine.UI). Installer: `InventoryModuleInstaller` (in `GlobalSceneInstaller`); the models are
bound in `DataInstaller`, the config in `ConfigurationInstaller`. Nothing in GrabModule references this module: the hand
talks to it through `IHeldItemStorage`, which `PlayerInventory` binds on the hand (`GrabController.UseStorage`).

## Key places
| What | Where |
|---|---|
| Storable flag | `Grabbable.CanStoreInInventory` (inspector "Can Store In Inventory"): on for the four hand weapon items and the wrench and hammer, off for ship modules and everything else. Turning it on is all an item needs. |
| Slot display | `IInventoryItemDisplay` (GrabModule) on the item: `HandWeaponItem` and `RepairTool` give their definition's icon and name. An item without it shows its object name. |
| Synced state | `PlayerInventory` model (`PerPlayer`, generated from `NetworkModelModule/GameResources/Definitions/PlayerInventory.asset`): `SlotItemNetIds` (`List<uint>`, 0 = empty, `SlotCount` entries), `SelectedSlot`, `IsStowed`. |
| Bridge and server rules | `PlayerInventory` (on `DummyPlayer`): pickup, slot switch (`CmdSelectSlot`), throw (`CmdThrow`), drop everything; it hands the slots to `HeldItemRegistry.SetCarried` on every peer. |
| Slot rules | `InventorySlots` (pure, tested): pickup slot, select / stow toggle, wheel cycle, hand item. |
| Input | `LocalInventoryInput` (on `DummyPlayer`): `SelectSlot1..4`, `SelectSlotScroll`, and `Release` (Q) as tap or hold (`ReleasePress`). |
| HUD | `InventoryView` in `GameHudWindow.prefab` (bottom centre), `InventoryPresenter`, slot prefab `GameResources/Prefabs/InventorySlotView.prefab` (9-sliced rounded sprites in `GameResources/Sprites`), `LocalInventoryBindingModel` (the local inventory and the predicted slot). |
| Config | `GameResources/InventoryConfiguration_Default` (Addressables, `Configurations`): `SlotCount` 4, slot view prefab, `ThrowHoldSeconds` 0.3, `ThrowSpeed` 9 m/s, `DropScatterRadius` 0.4 m, selected scale 1.15, slot animation 20/s (~0.1 s), icon fade 10/s, stowed icon alpha 0.35. |

## Behaviour
- **Hand = selected slot.** An empty selected slot is an empty hand. Pressing the selected slot's number again stows its
  item (hand empty, item stays in the slot; its icon dims) and once more takes it out. Another slot always shows its item.
- **Stowed items** (in a slot, not in the hand) are applied by `HeldItemRegistry` on every peer from the model:
  `Grabbable.ApplyStow` = the hold (kinematic, no collisions, network body off, follows the hand point) plus hidden
  renderers and disabled colliders (`StowedItemVisibility`, which restores each one's own state). `CanBeGrabbed` is false,
  so nobody can hover or grab it, the overboard drift, the deck and explosions skip it. A switch moves an item between
  held and stowed without a release: `HeldItemRegistry.OnItemReleased` does not fire, the deck does not catch it.
- **Pickup (E)** of a storable item: into the selected slot if empty, else into the first free slot (the selection moves
  there, the previous hand item is stowed); all full = refused (no highlight, as with a busy hand). A non-storable item
  (module) only into an empty hand; while it is held, slot keys and the wheel are ignored (the server refuses too).
- **Wheel**: one slot per notch, wraps around, no empty-hand step.
- **Prediction**: the HUD highlight moves at once (`LocalInventoryBindingModel.PredictSlot`); the server's
  `SelectedSlot` ends the prediction, a refusal comes back as `TargetRefuseSelection`.
- **Q**: released before `ThrowHoldSeconds` = drop under the hand as before (`LocalPlayerInteraction.ReleaseHeld`); held
  to `ThrowHoldSeconds` = throw along the camera view at `ThrowSpeed` plus the player's velocity (`CmdThrow`, the server
  sets the velocity). Both work for a non-storable item too. `LocalPlayerInteraction` no longer listens to `Release`, so
  only one of them fires.
- **The item leaves its slot** whenever it leaves the hand for the world (`GrabController.ServerReleaseHeld` asks the
  storage to forget it first): drop, throw, knockdown drop of a weapon, install / consume, destroyed under the hand. A
  stowed item that is despawned leaves its slot too (`HeldItemRegistry` → `GrabController.ServerForgetCarried`).
- **Death** (`PlayerLifeBody.ServerApplyDeath` → `IHeldItemRelease.ServerDropEverything`) and **leaving**
  (`PlayerInventory.OnStopServer`) drop every slot item around the hand point within `DropScatterRadius`. A rejoining
  player's entry is reset to empty on spawn (`ServerResetSlots`); nothing is saved.
- Input is ignored while the controls are blocked (a window, death), seated (`IsHeld`) or knocked down.

## Known limits
- Checked in Play mode on the host only (pickup rules, keys, wheel, stow toggle, fire after switching, Q tap / hold,
  module in hand, a stowed item despawned, the death drop). Two players (client view of stowed items, disconnect,
  rejoin) are not checked in Play mode yet.
- Slot icons are the definitions' `Icon` sprites, rendered once from the item models (256×256, transparent, 3/4 view;
  `GameResources/Icons`). The shop rows show the same icons. A new item without an icon shows its name.
- The HUD's interaction tooltip and spectator bar were moved up (tooltip y 28 → 118, spectator y 72 → 124) to make room
  for the bar.

## Tests
- `Features.InventoryModule.Editor.Tests`: `InventorySlotsTests` (pickup slot, full, stow toggle, another slot shows its
  item, wheel wrap, lookups), `ReleasePressTests` (tap, hold once, cancel), `InventoryGuardTests` (every hand weapon and
  tool the shop sells is storable and has a slot display, no module item is storable, the config loads with
  `SlotCount` ≥ 1, a slot view and a throw speed).
