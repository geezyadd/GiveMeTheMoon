# HandWeaponModule — weapons carried in the hand

The pistol and every later weapon a player holds in the hand like a tool. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file. The weapon core and its one network path are [WeaponModule](../WeaponModule/AGENTS.md).

## Assembly
`Features.HandWeaponModule` (layer 4; references: Grab, Camera, CharacterMovable, Input, InputRealization, NetworkModel, WeaponModule, Mirror, Zenject). No installer: `HandWeaponWielder` sits on the player prefab and is injected with it.

## Key places
| What | Where |
|---|---|
| Hand weapons | A hand weapon is one `HandWeaponDefinition` (`GameResources/PistolHandWeapon`: id, name, icon, description, price, item prefab, the `WeaponDefinition` it fires, `MinTriggerReportSeconds`, `CrosshairLayers`), sold by the shop after the tools (`ShopCatalog.HandWeapons`). The item (`GameResources/PistolItem`: primitives, `Grabbable`, `NetworkIdentity`, `HandWeaponItem`, the feel `HandGunFeel` from WeaponModule, `InteractionTooltip` with `Tooltip_Pistol`, `ZenAutoInjecter` for the feel's camera shake) is picked up, held and dropped like the wrench, and is in the network manager's spawn prefabs. `HandWeaponItem` binds the feel once, in a feel frame of its own in the item's scene (`OpenAirFeelFloor`), so a tracer does not swing with the hand. **`HandWeaponWielder`** (player prefab, every peer) watches the hand (`GrabController.Held`): while it holds a hand weapon item, on the server the player holds that weapon (`WeaponHolderSync.ServerBeginHolding`, so the hit reports are checked as for the turret); on the owner's peer the `Fire` input (LMB, `HandWeaponOwnerInput`; off while a window frees the cursor or the controls are blocked) is the trigger, `HandWeaponTriggerLatch` keeps a click reported at least `MinTriggerReportSeconds` (0.25 s) so every peer sees it, the shot leaves the muzzle toward the point under the crosshair (`CrosshairAim`: the view's centre ray on `CrosshairLayers`), the aim is sent in the player's own frame with the trigger (`OwnerSendAim`, only when the trigger changes) and the HUD ring shows the cooldown (`HeldWeaponReadinessModel`); every other peer replays the shot from the synced trigger and aim (`HandWeaponHolder`). Letting go, dropping or losing the item ends all of it. |

## The pistol
150 in the shop; `Pistol` weapon: one shot per press (`SingleShotFireMode`, cooldown = 1 / 0.5 shots per second = 2 s; a press within the cooldown is lost, holding never fires again), hitscan on `FieldRock`, 12 damage (breaks any small rock: at most 12 HP; pops every balloon: at most 3 HP), 150 m. Feel (`PistolFeelProfile`): muzzle flash, recoil kick (5 cm, 9°), tracer every shot, impact burst, `PistolShot` placeholder sound, view kick `CameraShake_PistolShot` for the holder.

## Known limits
- In the third-person view the player's body covers most of the pistol in hand; there is no first-person arm model yet.
- A rejoining player's hand is empty (the item drops on leaving), as for every item.

## Tests
- `Features.HandWeaponModule.Editor.Tests`: `HandWeaponTriggerLatchTests` (a short click stays reported for the minimum time, a held button until let go), `HandWeaponShopGuardTests` (every hand weapon item can be held, points back to its definition, has a feel, an injector and crosshair layers, fires a catalog weapon; the pistol: a press within 2 s is blocked, after 2 s it fires, one shot breaks any small rock and pops any balloon). The spawn prefab guard is in `ModuleCatalogGuardTests`. Play: `LoopSmokePistolCheck`, the pistol step of `CoopTwoProcessTest`.
