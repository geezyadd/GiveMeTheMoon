#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;
using Features.DebugModule.Scripts.UI;
using Features.HandWeaponModule.Scripts;
using Features.ShipDamageModule.Scripts.Repair;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;
using Features.ShopModule.Scripts.Network;
using Features.ShopModule.Scripts.UI;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // The client bot at the station's kiosk and in the debug window, as a player clicks them: opens the kiosk by aim and
    // interact, wheels the list to a row, clicks its Buy button (a click on a disabled button does nothing), sends a
    // purchase the button refuses as a modified client could, walks away, and tops the wallet up with "+1000".
    public sealed class CoopClientShop {
        private const float KIOSK_APPROACH_METERS = 2.5f;
        private const float WALK_AWAY_METERS = 8f;
        private const float STAND_HEIGHT = 0.8f;
        private const float SYNC_WAIT_SECONDS = 1f;
        // One mouse wheel notch as the UI input module reports it (InputSystemUIInputModule.scrollDeltaPerTick).
        private const float WHEEL_NOTCH = 6f;
        private const int MAX_WHEEL_NOTCHES = 40;
        private const float VIEWPORT_TOLERANCE = 1f;
        private const string MONEY_LABEL_FORMAT = "+{0}";
        private const string SCROLLED_SCREENSHOT = "shop-scrolled";
        private const string MONEY_SCREENSHOT = "debug-money";

        private readonly CoopTestClient _client;
        private readonly CoopClientHands _hands;
        private readonly CoopClientScreenshots _screenshots;
        private readonly WaitForSeconds _syncWait = new WaitForSeconds(SYNC_WAIT_SECONDS);
        private bool _hasScrolledScreenshot;

        private static ShopModel Shop => SessionQueries.Resolve<ShopModel>();
        private static IReadOnlyWalletModel Wallet => SessionQueries.Resolve<IReadOnlyWalletModel>();

        public CoopClientShop(CoopTestClient client, CoopClientHands hands, CoopClientScreenshots screenshots) {
            _client = client;
            _hands = hands;
            _screenshots = screenshots;
        }

        // The Buy button of an entry the crew cannot afford is disabled; the purchase sent anyway is refused by the server.
        public IEnumerator TryUnaffordableCoroutine(string entryId) {
            yield return OpenCoroutine();
            int entry = FindEntry(entryId, out long price);
            Button buy = FindBuyButton(entry);
            yield return WheelIntoViewCoroutine(buy);
            if (buy.interactable)
                yield return _client.FailCoroutine($"The Buy button of {entryId} ({price}) is enabled with {Wallet.Balance} in the wallet.");

            Click(buy);
            SessionQueries.Resolve<ShopPurchaseRequestEventClass>().InvokePurchaseRequested(entry);
            yield return _syncWait;
            Report(CoopWeaponsEconomySteps.PURCHASE_REFUSED, entryId);
        }

        public IEnumerator BuyCoroutine(string entryId) {
            yield return OpenCoroutine();
            int entry = FindEntry(entryId, out long price);
            Button buy = FindBuyButton(entry);
            yield return WheelIntoViewCoroutine(buy);
            long expected = Wallet.Balance - price;
            Click(buy);
            yield return _client.WaitForCoroutine(() => Wallet.Balance == expected, $"the synced balance to drop by {price} after buying {entryId}");
            // The delivered item spawns on this peer a moment after the balance.
            yield return _syncWait;
            Report(CoopWeaponsEconomySteps.BOUGHT, entryId);
        }

        public IEnumerator LeaveCoroutine() {
            ShopKioskInteractable kiosk = FindKiosk();
            yield return _hands.StandAtCoroutine(kiosk.transform.position + FlatAway(kiosk) * WALK_AWAY_METERS + Vector3.up * STAND_HEIGHT);
            yield return _client.WaitForCoroutine(() => Shop.IsOpen == false, "the shop window closed after walking away");
            Report(CoopWeaponsEconomySteps.SHOP_CLOSED, string.Empty);
        }

        // The debug window's Shop -> "+1000" through its corner button, as the loop test's LoopSmokeDebugMoneyGrant does.
        public IEnumerator GrantMoneyCoroutine() {
            long amount = SessionQueries.Resolve<WalletConfiguration>().DebugGrantAmount;
            long expected = Wallet.Balance + amount;
            DebugPanelModel panel = SessionQueries.Resolve<DebugPanelModel>();
            DebugToggleView toggle = Object.FindAnyObjectByType<DebugToggleView>();
            if (toggle == null)
                yield return _client.FailCoroutine("No debug corner button on the client.");

            Click(toggle.DebugButton);
            yield return _client.WaitForCoroutine(() => panel.IsOpen, "the debug panel opened by its corner button");
            string label = string.Format(CultureInfo.InvariantCulture, MONEY_LABEL_FORMAT, amount);
            if (TryFindDebugRow(label, out DebugActionRowItem row) == false)
                yield return _client.FailCoroutine($"The client's debug panel shows no \"{label}\" button.");

            Click(row.DebugButton);
            yield return _client.WaitForCoroutine(() => Wallet.Balance == expected, $"the synced balance to grow by {amount} from the client's debug button");
            row.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0f;
            yield return _screenshots.CaptureCoroutine(MONEY_SCREENSHOT);
            Click(toggle.DebugButton);
            yield return _client.WaitForCoroutine(() => panel.IsOpen == false, "the debug panel closed by its corner button");
            Report(CoopWeaponsEconomySteps.MONEY_GRANTED, label);
        }

        private IEnumerator OpenCoroutine() {
            if (Shop.IsOpen)
                yield break;

            ShopKioskInteractable kiosk = FindKiosk();
            yield return _hands.StandAtCoroutine(kiosk.transform.position + FlatAway(kiosk) * KIOSK_APPROACH_METERS + Vector3.up * STAND_HEIGHT);
            yield return _hands.UseAimedCoroutine(kiosk, CounterCenter(kiosk), "the shop kiosk");
            yield return _client.WaitForCoroutine(() => Shop.IsOpen, "the shop window opened by interact at the kiosk");
        }

        // The wheel over an item row scrolls the list, down or up, until the button's row is in view.
        private IEnumerator WheelIntoViewCoroutine(Button buy) {
            RectTransform row = (RectTransform)buy.GetComponentInParent<ShopItemRow>().transform;
            ScrollRect scroll = row.GetComponentInParent<ScrollRect>();
            bool isBelow = WorldRect(row).yMin < WorldRect(scroll.viewport).yMin;
            PointerEventData wheel = new PointerEventData(EventSystem.current) { scrollDelta = (isBelow ? Vector2.down : Vector2.up) * WHEEL_NOTCH };
            for (int i = 0; i < MAX_WHEEL_NOTCHES && IsInsideViewport(row, scroll.viewport) == false; i++) {
                ExecuteEvents.ExecuteHierarchy(row.gameObject, wheel, ExecuteEvents.scrollHandler);
                yield return null;
            }

            if (IsInsideViewport(row, scroll.viewport) == false)
                yield return _client.FailCoroutine($"The mouse wheel does not bring the shop row {row.name} into view.");

            if (_hasScrolledScreenshot || scroll.verticalNormalizedPosition > 0.5f)
                yield break;

            _hasScrolledScreenshot = true;
            yield return _screenshots.CaptureCoroutine(SCROLLED_SCREENSHOT);
        }

        private void Report(string step, string message) {
            CoopObservation observation = CoopWeaponsEconomyObserver.Capture(step);
            observation.Message = message;
            _client.Report(observation);
        }

        // A pointer click through the event system: a disabled button ignores it, as it ignores the mouse.
        private static void Click(Selectable button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        private static bool TryFindDebugRow(string label, out DebugActionRowItem row) {
            row = null;
            DebugPanelView view = Object.FindAnyObjectByType<DebugPanelView>();
            return SessionQueries.Resolve<DebugActionsModel>().TryFind(DebugSections.SHOP, label, out DebugActionDefinition action)
                   && view != null && view.TryGetDebugRow(action, out row);
        }

        private static int FindEntry(string entryId, out long price) {
            ShopCatalog catalog = SessionQueries.Resolve<ShopCatalog>();
            int entry = 0;
            foreach (ModuleDefinition module in catalog.Modules) {
                price = module.Price;
                if (module.Id == entryId)
                    return entry;

                entry++;
            }

            foreach (RepairToolDefinition tool in catalog.Tools) {
                price = tool.Price;
                if (tool.Id == entryId)
                    return entry;

                entry++;
            }

            foreach (HandWeaponDefinition weapon in catalog.HandWeapons) {
                price = weapon.Price;
                if (weapon.Id == entryId)
                    return entry;

                entry++;
            }

            throw new KeyNotFoundException($"The shop does not sell {entryId}.");
        }

        private static Button FindBuyButton(int entry) {
            List<ShopItemRow> active = new List<ShopItemRow>();
            foreach (ShopItemRow row in Object.FindObjectsByType<ShopItemRow>(FindObjectsSortMode.None)) {
                if (row.isActiveAndEnabled)
                    active.Add(row);
            }

            active.Sort((left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
            return active[entry].GetComponentInChildren<Button>();
        }

        private static bool IsInsideViewport(RectTransform row, RectTransform viewport) {
            Rect rowRect = WorldRect(row);
            Rect viewportRect = WorldRect(viewport);
            return rowRect.yMin >= viewportRect.yMin - VIEWPORT_TOLERANCE && rowRect.yMax <= viewportRect.yMax + VIEWPORT_TOLERANCE;
        }

        private static Rect WorldRect(RectTransform rectTransform) {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        // The kiosk of the pad the ship stands on: the nearest one.
        public static ShopKioskInteractable FindKiosk() {
            Vector3 ship = Object.FindAnyObjectByType<ShipBase>().transform.position;
            ShopKioskInteractable nearest = null;
            foreach (ShopKioskInteractable kiosk in Object.FindObjectsByType<ShopKioskInteractable>(FindObjectsSortMode.None)) {
                if (nearest == null || (kiosk.transform.position - ship).sqrMagnitude < (nearest.transform.position - ship).sqrMagnitude)
                    nearest = kiosk;
            }

            return nearest;
        }

        // From the kiosk toward the ship: the player comes from the deck and walks back there.
        private static Vector3 FlatAway(ShopKioskInteractable kiosk) {
            Vector3 away = Object.FindAnyObjectByType<ShipBase>().transform.position - kiosk.transform.position;
            away.y = 0f;
            return away.normalized;
        }

        private static Vector3 CounterCenter(ShopKioskInteractable kiosk) {
            foreach (Collider collider in kiosk.GetComponentsInChildren<Collider>()) {
                if (collider.isTrigger == false)
                    return collider.bounds.center;
            }

            throw new MissingComponentException("The shop kiosk has no solid counter.");
        }
    }
}
#endif
