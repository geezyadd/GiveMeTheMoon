# ShopModule — shop, money and buy-back

The station shop kiosk, the crew wallet and buying back a dead crewmate. Read the [root AGENTS.md](../../../../AGENTS.md) first (game, rules, module map). Change this module's description here, not in the root file.

## Assembly
`Features.ShopModule` (layer 6; references: PlayerLife, Ship, ShipDamage for the repair tools, Camera, GameFlow, Mvp, NetworkModel, PlayerProfileModule.Data, FlightFieldModule.Contracts for the ore rewards).

## Loop status
- the only income is the ore of broken rocks pulled in with the harpoon or flown into (see "Ore" in [FlightFieldModule](../FlightFieldModule/AGENTS.md):
  12 / 25 / 60 per piece, about 18 on average, a good flight about 10-14 pieces, 150-250; the target balloons were removed on
  2026-10-28) and the delivery parcels ([ParcelModule](../ParcelModule/AGENTS.md): 350-550 paid through `ServerEarn` when
  the ship lands at the parcel's station with it on board); the rest is the
  starting balance (`WalletConfiguration`, 300 for a normal run since 2026-10-26, was 1 000 000; a stage preset brings
  its own); there are no free station drops in a normal run, so engines and guns are bought;
- debug money: the debug window's Shop → "+1000" (`ShopDebugMoneySystem`, editor / development builds, any player)
  raises `ShopDebugMoneyRequestEventClass`; `CrewWallet.CmdDebugGrantMoney` makes the server add
  `WalletConfiguration.DebugGrantAmount` (1 000) through `ServerEarn`;
- the shop window (`ShopWindow.prefab`) lists the Crew section and every catalog entry in a scroll view (`ShopPanel/Scroll`:
  `Viewport` with `RectMask2D`, `Content` with a vertical layout and a `ContentSizeFitter`, an auto-hiding vertical
  `Scrollbar`; mouse wheel sensitivity 20 = about one row per notch with the Input System UI module), so a longer
  catalog scrolls instead of spilling out of the panel; the 1200 x 960 panel fits the 1920 x 1080 windows canvas
  (`WindowsRootService`, every 16:9 screen, 1280 x 720 included);

## Key places
| What | Where |
|---|---|
| Shop and money | `Assets/Features/ShopModule/` (`CrewWallet`, `ShopCatalog`, `WalletConfiguration`); income: `CrewWallet.ServerEarn` (the server reaches the wallet through `ICrewWalletBindingModel`), paid by `FieldOreRewardSystem` for every piece of ore that reached the ship; the HUD balance flashes on a rise; the kiosk `ShopKioskInteractable` is part of the `StationPad` prefab, `ShopAccessService` / `ShopAccessRule` decide open, keep-open and purchase (Build, alive, distance from `ShopKioskConfiguration`), the server keeps the opened kiosk per player in `ShopVisitModel`; the debug window's Shop → "Open the shop here" opens it anywhere, only in editor / development builds |
| Hand weapons | `ShopCatalog.HandWeapons` (`HandWeaponDefinition`, [HandWeaponModule](../HandWeaponModule/AGENTS.md): the pistol 150, the harpoon 120): listed after the tools (entry index = modules, then tools, then hand weapons), stats line "Damage, one shot every N s, range"; bought like a tool, the item lands on the delivery pad |
| Buying back a dead crewmate | the shop window's "Crew" section (`CrewReviveView` / `CrewRevivePresenter`, list `DeadCrewModel`, kept by `DeadCrewSystem` from the synced `PlayerLifeRegistry`) → `CrewWallet.CmdRevive` → `ReviveShopSystem` (kiosk check like a purchase, `ReviveRule`, then `ServerTrySpend`, then `IPlayerLifeReviver.ServerRevive(PlayerKey)`); price in `ReviveConfiguration`; the revived body is teleported to the ship's next `ShipDeckSpawnPoint` in turn (`IShipDeckSpawnPoints`); `ReviveConfigurationValidator` fails startup on a price <= 0 |

The shop wallet is the `Wallet` model (`long Balance`). The start value comes from `WalletConfiguration`, a purchase lowers the balance, HUD and shop read `IReadOnlyWalletModel`.

## Tests
- `Features.ShopModule.Editor.Tests` (incl. `FieldOreRewardSystemTests`: ore that reached the ship pays its price once; `ShopWindowScrollTests`: the shipped window with 12 items stacks its rows without overlap, the wheel over a row and the scrollbar scroll the list to the last item, the panel fits the windows canvas)
