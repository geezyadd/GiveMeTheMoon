# TurretModule — the minigun turret's seat station

The turret's seat station `ShipTurretStation` and its pure parts. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
`Features.TurretModule` (layer 5; references: Ship, Camera, Grab, Input, NetworkModel, WeaponModule, WeaponModule.Contracts, Mirror, Zenject): the turret's seat station `ShipTurretStation` and its pure parts; it has no bindings, so no installer: its MonoBehaviours on the slot view are injected by the scene. The weapon itself is [WeaponModule](../WeaponModule/AGENTS.md); the seat and socket are [ShipModule](../ShipModule/AGENTS.md).

## Key places
| What | Where |
|---|---|
| Minigun turret on the ship | `MinigunModule` (slot `Turret`, removable, 600, in the shop, in the station drops, in the `Mid` / `Late` presets) on socket `turret-front` (`ShipPlatform/TurretSocket`, front-left of the deck, mirroring the radar; its child `SeatInteractable` is the seat collider behind the gun). The slot view `MinigunSlotView` (primitives: tripod, yaw pivot, pitch pivot with drum, spinning barrels, ring-and-cross sight, V grips, cyan gloves shown while someone sits; a `ShipSeat`) carries `ShipTurretStation` (`TurretModule/Scripts/`, assembly `Features.TurretModule`), the turret's `ISeatStation`: the socket tells it on every peer who sits there by player key (`ShipSocket.SyncStationOccupant`; the station finds that player with `IShipRiders.TryFindByKey`; no role branch in `ShipRider` / `ShipSeats`); on the owner's peer the mouse turns it at once (`TurretAim`, limits and sensitivity in `MinigunTurretDefinition`: yaw free, pitch -10..75), the `Fire` input (LMB / right trigger) fires, interact or jump stands up (`TurretOwnerInput`, `ShipRider.OwnerRequestStand`) and the camera rides the gun's camera point (`IGameCameraService.BeginFixedView` / `EndFixedView`: FP camera, mouse look off); the seat point (`SitPoint`) sits on the yaw pivot and the station has the seated rider follow it on every peer (`ShipRider.FollowSeatPoint`, from the aim that peer shows, no extra messages), so the gunner's body turns with the gun; the owner's own body renderers cast only shadows while it sits (`SeatedOwnerBody`: at a steep pitch the camera drops below the body), and standing up leaves the player where the seat point was, clamped onto the deck walk (`SeatStandingSpot`; only a rider that followed a moving seat point, a fixed seat keeps its spot); other peers smooth the synced aim (`TurretAimSmoothing`) and replay the fire. The station talks to its weapon model's feel through `IWeaponFeelPresenter` (`MinigunFeel` is one), so another gun model brings its own presenter |

## Known limits
- **The minigun breaks field rocks** (see "Weapon damage on rocks" in "Breakable rocks" of [FlightFieldModule](../FlightFieldModule/AGENTS.md)); a hit rock has no damaged
  look between hits (only the flash and shake while it is struck), and a client's rock hits were checked only on the
  host with the host as the gunner, not with a second client process. A player who joins at a station
  sees an installed turret from `ShipSockets` and its gunner from the socket's `OccupantKey` (the gunner's `PlayerKey`); the turret shows the
  gunner's last synced aim, and a gunner who leaves frees the seat and stops firing. Remote fire was checked on the
  host with the loop test's bot as the gunner, not yet with a second client process.

## Tests
- `Features.TurretModule.Editor.Tests`, the turret's pure parts: `TurretAimTests` (mouse to yaw / pitch, limits, wrap, the synced direction round trip), `TurretAimSmoothingTests`, `TurretAimSendScheduleTests`, `TurretOwnerInputTests` (interact stands up, a free cursor keeps the mouse and the trigger off the turret), `SeatedOwnerBodyTests` (the owner's hidden body keeps its shadow and comes back as it was), `SeatStandingSpotTests` (standing up from the turret clamps the spot onto the deck, a fixed seat keeps it).
