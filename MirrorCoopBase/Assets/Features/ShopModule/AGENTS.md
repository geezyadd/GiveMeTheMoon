# ShopModule — shop, money and buy-back

The station shop kiosk, the crew wallet and buying back a dead crewmate. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
`Features.ShopModule` (layer 6; references: PlayerLife, Ship, ShipDamage for the repair tools, Camera, GameFlow, Mvp, NetworkModel, PlayerProfileModule.Data, FlightFieldModule.Contracts for the balloon rewards).

## Loop status
- the only income is the target balloons of the flight field (see "Target balloons" in [FlightFieldModule](../FlightFieldModule/AGENTS.md): 20 / 50 / 200 per
  balloon, 6-10 balloons per flight on loop 0); the rest is the starting balance (`WalletConfiguration`, 1 000 000 now;
  a stage preset brings its own);

## Key places
| What | Where |
|---|---|
| Shop and money | `Assets/Features/ShopModule/` (`CrewWallet`, `ShopCatalog`, `WalletConfiguration`); income: `CrewWallet.ServerEarn` (the server reaches the wallet through `ICrewWalletBindingModel`), paid by `FieldTargetRewardSystem` for every popped target balloon; the HUD balance flashes on a rise; the kiosk `ShopKioskInteractable` is part of the `StationPad` prefab, `ShopAccessService` / `ShopAccessRule` decide open, keep-open and purchase (Build, alive, distance from `ShopKioskConfiguration`), the server keeps the opened kiosk per player in `ShopVisitModel`; the debug window's Shop → "Open the shop here" opens it anywhere, only in editor / development builds |
| Buying back a dead crewmate | the shop window's "Crew" section (`CrewReviveView` / `CrewRevivePresenter`, list `DeadCrewModel`, kept by `DeadCrewSystem` from the synced `PlayerLifeRegistry`) → `CrewWallet.CmdRevive` → `ReviveShopSystem` (kiosk check like a purchase, `ReviveRule`, then `ServerTrySpend`, then `IPlayerLifeReviver.ServerRevive(PlayerKey)`); price in `ReviveConfiguration`; the revived body is teleported to the ship's next `ShipDeckSpawnPoint` in turn (`IShipDeckSpawnPoints`); `ReviveConfigurationValidator` fails startup on a price <= 0 |

The shop wallet is the `Wallet` model (`long Balance`). The start value comes from `WalletConfiguration`, a purchase lowers the balance, HUD and shop read `IReadOnlyWalletModel`.

## Tests
- `Features.ShopModule.Editor.Tests` (incl. `FieldTargetRewardSystemTests`: a pop pays its reward once)
