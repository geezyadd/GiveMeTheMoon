# HandWeaponModule — weapons carried in the hand

The pistol and every later weapon a player holds in the hand like a tool. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file. The weapon core and its one network path are [WeaponModule](../WeaponModule/AGENTS.md).

## Assembly
`Features.HandWeaponModule` (layer 4; references: Grab, Camera, CharacterMovable, Input, InputRealization, NetworkModel, WeaponModule, Mirror, Zenject). No installer: `HandWeaponWielder` sits on the player prefab and is injected with it.

## Key places
| What | Where |
|---|---|
| Hand weapons | A hand weapon is one `HandWeaponDefinition` (`GameResources/PistolHandWeapon`: id, name, icon, description, price, item prefab, the `WeaponDefinition` it fires, `MinTriggerReportSeconds`, `CrosshairLayers`), sold by the shop after the tools (`ShopCatalog.HandWeapons`). The item (`GameResources/PistolItem`: primitives, `Grabbable`, `NetworkIdentity`, `HandWeaponItem`, the feel `HandGunFeel` from WeaponModule, `InteractionTooltip` with `Tooltip_Pistol`, `ZenAutoInjecter` for the feel's camera shake) is picked up, held and dropped like the wrench, and is in the network manager's spawn prefabs. `HandWeaponItem` (an `IWeapon`; `HandDefinition` is its hand weapon, `Definition` the weapon it fires) binds the feel once, in a feel frame of its own in the item's scene (`OpenAirFeelFloor`), so a tracer does not swing with the hand. **`HandWeaponWielder`** (player prefab, every peer) watches the hand (`GrabController.Held`): while it holds a hand weapon item it equips the item on the player's `WeaponHolderSync` (`Equip`, every peer; the item is an `IWeapon`, its magazine a `WeaponMagazine` on the item), so the local player's `WeaponSystem` fires it (LMB) and reloads it (R); on the owner's peer the weapon aims from the palm (not the muzzle, which turns with it: aiming from it fed back and made a rifle pressed to the floor or a player spin every frame, fixed 2026-11-26) at the point under the crosshair (`CrosshairAim`: the view's centre ray on `CrosshairLayers`, through the holder's own body; close to the hand the aim eases over to the view's direction: fully along the view under `MinAimDistance` 1.5 m, fully at the point from 3 m, a smooth blend between, and a point beside or behind the hand fades out alike; a hard switch at 1.5 m made a lowered pistol snap), and the shot leaves the muzzle along that aim and the aim goes to the holder (`OwnerSetAim`) and out to the other peers as the view turns (`HandWeaponAimSync`: at most every `AimSendIntervalSeconds` 0.05 s, once it turned `AimSendMinDegrees` 1°; until 2026-11-26 it went only with a trigger change, so others saw the gun point at the last shot); every other peer turns the model toward the synced aim over `RemoteAimSmoothingSeconds` 0.08 s (`HandWeaponHolder`). Each item's box collider is fitted to its model. Letting go, dropping or losing the item ends all of it (`Unequip`). The old trigger latch and owner input are gone: every shot now goes to the server and to every peer as its own message (WeaponModule, "The weapon system"). |

## The pistol
150 in the shop; `Pistol` weapon: one shot per press (`SingleShotFireMode`, cooldown = 1 / 3 shots per second = 0.33 s since 2026-11-26, was 2 s; a press within the cooldown is lost, holding never fires again), unlimited rounds (magazine capacity 0), hitscan on FieldRock, PlayerRagdoll, Default and Ground (was FieldRock only), 12 damage (breaks any small rock: at most 12 HP; pops every balloon: at most 3 HP), 150 m, a struck player is pushed 8 m/s. The other hand weapons (knockdown gun, grenade and rocket launchers) are in WeaponModule's "Hand weapons now". Feel (`PistolFeelProfile`): a small muzzle flash per shot, recoil kick (5 cm, 9°), tracer every shot, impact burst, view kick `CameraShake_PistolShot` for the holder; no sound.

## The weapon in the hand (2026-11-26: one pose for everyone)
The weapon sits in the holder's **real right hand**, the same in the holder's first- and third-person views and on every
other peer (until 2026-11-26 it had two poses: an offset in the holder's first-person view and an offset beside the
arm for everyone else). `HandWeaponGrip` (on `DummyPlayer`; refs: the ragdoll model's `LimbReach` and its right upper
arm, forearm, hand and middle finger bones) takes a `LimbReach` request for `RightHand` at priority -1 (a slap, priority
0 and newer, still takes the hand) while a weapon is held, on every peer that simulates the body, and every physics step
sets its goal to the shoulder + the aim × `Reach Share` (0.85) of the arm's length: the arm reaches along the aim.
`GripPoint` is the palm (`Palm Share` 0.5 between the wrist and the middle finger bone). `HandWeaponPoseDriver` places the
model (`HandWeaponPose.AtGrip`) with the weapon's `HandWeaponDefinition.GripOffset` (the middle of its grip, in the
model's frame) in the palm and the barrel along the aim, upright; on the holder's own peer right after the look is
applied, elsewhere every frame. The shot leaves the muzzle where the model is shown. In first person the own body is
still hidden (`OwnBodyHider`), so the holder sees the weapon at the hand's place without the arm.

Models are 2× the FBX size (`<Name>View` scale 2, 2026-11-26) so they match the cartoon hands; grip offsets: pistol
(0, -0.12, -0.13), knockdown gun (0, -0.16, -0.05), grenade launcher (0, -0.2, 0.01), rocket launcher (0, -0.21, -0.12),
measured from each model's lower rear (the grip). The FBX are imported Read/Write (QuickOutline reads the mesh). In the
lobby the four lie in a row along the X axis (x -3.8..-1.7, z 1.2).

## Known limits
- A hand weapon placed in a scene (the lobby rack) must not carry `ZenAutoInjecter`: the scene context injects it, and
  the injector's exception stops the injection of the whole scene. The prefabs keep it for items spawned at runtime.
- In first person the arm is not drawn (the whole own body is hidden), only the weapon at the hand.
- The arm follows physics: a hit, a jump or a knockdown moves the weapon with the hand.
- A rejoining player's hand is empty (the item drops on leaving), as for every item.

## Tests
- `Features.HandWeaponModule.Editor.Tests`: `HandWeaponPoseTests` (the grip lands in the palm, the barrel points along the aim and stays upright, an empty aim keeps it along the arm point), `CrosshairAimTests` (a far point is aimed at from the hand, a point at the barrel or behind the hand gives the view's direction, lowering the view onto the floor turns the aim without a jump), `HandWeaponAimSyncTests` (the aim goes out as it turns, not within the interval, not for a tiny turn; other peers turn toward it without snapping), `HandWeaponShopGuardTests` (every hand weapon item can be held, points back to its definition, has a feel, an injector and crosshair layers, fires a catalog weapon; the pistol: a press within its 1/3 s cooldown is blocked, after it it fires, one shot breaks any small rock and pops any balloon). The spawn prefab guard is in `ModuleCatalogGuardTests`. Play: `LoopSmokePistolCheck`, the pistol step of `CoopTwoProcessTest`.
