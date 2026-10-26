#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;
using Features.ShopModule.Scripts.Network;
using Features.ShopModule.Scripts.UI;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // Uses the station's shop kiosk the way a player does: aims at it, presses interact, buys from the window and
    // walks away. Then sends a purchase from afar, as a modified client could, which the server must refuse.
    public sealed class LoopSmokeShopCheck {
        private const int PURCHASES = 5;
        // Propeller: the cheapest catalog entry.
        private const int BOUGHT_ENTRY = 2;
        private const float APPROACH_DISTANCE = 2.5f;
        private const float WALK_AWAY_DISTANCE = 8f;
        private const float STAND_HEIGHT = 0.8f;
        private const float STAND_SETTLE_SECONDS = 1f;
        private const float ITEMS_REST_TIMEOUT_SECONDS = 8f;
        // A toppling stack passes through slow moments: the items rest only once they stay slow this long.
        private const float ITEMS_REST_HOLD_SECONDS = 0.5f;
        private const float COMMAND_TIMEOUT_SECONDS = 3f;
        private const float REJECTED_COMMAND_WAIT_SECONDS = 0.5f;
        private const float DELIVERY_AREA_RADIUS = 2.5f;
        private const float BELOW_DELIVERY_TOLERANCE = 1.2f;
        private const float SETTLED_SPEED = 0.3f;
        // One mouse wheel notch as the UI input module reports it (InputSystemUIInputModule.scrollDeltaPerTick).
        private const float WHEEL_NOTCH = 6f;
        private const int MAX_WHEEL_NOTCHES = 40;
        private const float VIEWPORT_TOLERANCE = 1f;
        private const string SCROLLED_SCREENSHOT = "shop-scrolled-to-last-item.png";

        private readonly LoopSmokeFlight _flight;

        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static LocalPlayerInteraction Interaction => Player.GetComponent<LocalPlayerInteraction>();

        public LoopSmokeShopCheck(LoopSmokeContext context) =>
            _flight = context.Flight;

        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 2, 5, 20)]
        public IEnumerator BuyAtKioskAndWalkAwayCoroutine() {
            ShipLandingPad pad = _flight.FindPadUnderShip();
            ShopKioskInteractable kiosk = pad.GetComponentInChildren<ShopKioskInteractable>();
            Assert.IsNotNull(kiosk, $"The pad {pad.name} has no shop kiosk.");
            ShopModel shop = SessionQueries.Resolve<ShopModel>();
            IReadOnlyWalletModel wallet = SessionQueries.Resolve<IReadOnlyWalletModel>();
            List<ShipItem> itemsBefore = FindAllItems();
            try {
                CameraLookDriver.DebugPitchOverride = true;
                yield return LoopSmokeDebugMoneyGrant.GrantThroughDebugWindowCoroutine();
                yield return OpenAtKioskCoroutine(kiosk, shop);
                yield return WheelToLastRowCoroutine();
                yield return BuyCoroutine(wallet);
                yield return WalkAwayCoroutine(kiosk, shop);
                yield return AssertDeliveredCoroutine(kiosk, itemsBefore);
                yield return AssertFarPurchaseRefusedCoroutine(wallet);
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }
        }

        internal static IEnumerator OpenAtKioskCoroutine(ShopKioskInteractable kiosk, ShopModel shop) {
            Vector3 front = kiosk.transform.position - FlatForward() * APPROACH_DISTANCE;
            yield return StandAtCoroutine(front + Vector3.up * STAND_HEIGHT);
            yield return LoopSmokeHandCheck.AimAtCoroutine(CounterCenter(kiosk));
            Assert.AreSame(kiosk, Interaction.Target.Usable, "The shop kiosk is not offered under the crosshair.");
            Assert.IsFalse(shop.IsOpen, "The shop window is open before interact.");

            Interaction.Interact();
            yield return WaitForCoroutine(() => shop.IsOpen, "the shop window opened by interact at the kiosk");
            Assert.AreSame(kiosk.transform, shop.Kiosk, "The shop window is not bound to the kiosk it was opened at.");
        }

        private static IEnumerator BuyCoroutine(IReadOnlyWalletModel wallet) {
            Button buy = FindBuyButton(BOUGHT_ENTRY);
            for (int i = 0; i < PURCHASES; i++) {
                long balance = wallet.Balance;
                buy.onClick.Invoke();
                yield return WaitForCoroutine(() => wallet.Balance < balance, $"the balance to drop after purchase {i + 1}");
            }
        }

        // The wheel over an item row scrolls the list (the row's button does not swallow it) until the last item is in
        // view: every catalog entry can be reached, however many the shop sells.
        private static IEnumerator WheelToLastRowCoroutine() {
            List<ShopItemRow> rows = FindActiveRows();
            ScrollRect scroll = rows[0].GetComponentInParent<ScrollRect>();
            Assert.IsNotNull(scroll, "The shop's item list is not in a scroll view.");
            RectTransform last = (RectTransform)rows[^1].transform;
            PointerEventData wheel = new PointerEventData(EventSystem.current) { scrollDelta = Vector2.down * WHEEL_NOTCH };
            for (int i = 0; i < MAX_WHEEL_NOTCHES && IsInsideViewport(last, scroll.viewport) == false; i++) {
                ExecuteEvents.ExecuteHierarchy(rows[0].gameObject, wheel, ExecuteEvents.scrollHandler);
                yield return null;
            }

            Assert.IsTrue(IsInsideViewport(last, scroll.viewport), "The mouse wheel does not bring the shop's last item into view.");
            yield return LoopSmokeReviveCheck.CaptureScreenshotCoroutine(SCROLLED_SCREENSHOT);
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

        private static IEnumerator WalkAwayCoroutine(ShopKioskInteractable kiosk, ShopModel shop) {
            Vector3 away = kiosk.transform.position - FlatForward() * WALK_AWAY_DISTANCE;
            yield return StandAtCoroutine(away + Vector3.up * STAND_HEIGHT);
            Assert.IsFalse(shop.IsOpen, "The shop window stayed open after the player walked away from the kiosk.");
        }

        private static IEnumerator AssertDeliveredCoroutine(ShopKioskInteractable kiosk, List<ShipItem> itemsBefore) {
            List<ShipItem> bought = FindAllItems();
            bought.RemoveAll(itemsBefore.Contains);
            Assert.AreEqual(PURCHASES, bought.Count, "Not every purchase spawned an item.");
            yield return WaitForItemsToRestCoroutine(CollectBodies(bought));

            float floor = kiosk.transform.position.y - BELOW_DELIVERY_TOLERANCE;
            foreach (ShipItem item in bought) {
                Assert.IsTrue(IsAtDeliveryPoint(item, kiosk.DeliveryPoint), $"{item.name} at {item.transform.position} does not lie at the kiosk's delivery point.");
                Assert.Greater(item.transform.position.y, floor, $"{item.name} fell through the pad.");
                Assert.IsTrue(item.GetComponent<Grabbable>().CanBeGrabbed, $"{item.name} cannot be picked up.");
            }
        }

        // The delivered stack topples onto the counter and slides for a few seconds, longer on a slow frame rate: a
        // fixed wait caught it still sliding. Items stuck in each other never come to rest, so the timeout still fails.
        private static IEnumerator WaitForItemsToRestCoroutine(List<Rigidbody> bodies) {
            float start = Time.time;
            float restingSince = start;
            while (Time.time - restingSince < ITEMS_REST_HOLD_SECONDS) {
                if (TryFindMovingBody(bodies, out Rigidbody moving)) {
                    restingSince = Time.time;
                    if (restingSince - start > ITEMS_REST_TIMEOUT_SECONDS)
                        Assert.Fail($"{moving.name} does not rest after {ITEMS_REST_TIMEOUT_SECONDS} s ({moving.linearVelocity.magnitude:F2} m/s): stuck in another item?");
                }

                yield return null;
            }

            Debug.Log($"[LoopSmoke] the bought items came to rest {restingSince - start:F2} s after the player walked away.");
        }

        private static List<Rigidbody> CollectBodies(List<ShipItem> items) {
            List<Rigidbody> bodies = new List<Rigidbody>(items.Count);
            foreach (ShipItem item in items)
                bodies.Add(item.GetComponent<Rigidbody>());

            return bodies;
        }

        private static bool TryFindMovingBody(List<Rigidbody> bodies, out Rigidbody moving) {
            moving = null;
            foreach (Rigidbody body in bodies) {
                if (body.linearVelocity.magnitude < SETTLED_SPEED)
                    continue;

                moving = body;
                return true;
            }

            return false;
        }

        private static IEnumerator AssertFarPurchaseRefusedCoroutine(IReadOnlyWalletModel wallet) {
            long balance = wallet.Balance;
            SessionQueries.Resolve<ShopPurchaseRequestEventClass>().InvokePurchaseRequested(BOUGHT_ENTRY);
            yield return new WaitForSeconds(REJECTED_COMMAND_WAIT_SECONDS);
            Assert.AreEqual(balance, wallet.Balance, "The server took money for a purchase far from the kiosk.");
        }

        private static IEnumerator StandAtCoroutine(Vector3 position) {
            Rigidbody body = Player.GetComponent<CharacterMovableBase>().Body;
            body.position = position;
            body.transform.position = position;
            body.linearVelocity = Vector3.zero;
            yield return new WaitForSeconds(STAND_SETTLE_SECONDS);
        }

        private static Button FindBuyButton(int entry) {
            List<ShopItemRow> active = FindActiveRows();
            Assert.Greater(active.Count, entry, "The shop window shows too few rows.");
            return active[entry].GetComponentInChildren<Button>();
        }

        private static List<ShopItemRow> FindActiveRows() {
            ShopItemRow[] rows = Object.FindObjectsByType<ShopItemRow>(FindObjectsSortMode.InstanceID);
            List<ShopItemRow> active = new List<ShopItemRow>();
            foreach (ShopItemRow row in rows) {
                if (row.isActiveAndEnabled)
                    active.Add(row);
            }

            active.Sort((left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
            Assert.IsNotEmpty(active, "The shop window shows no item rows.");
            return active;
        }

        private static List<ShipItem> FindAllItems() =>
            new List<ShipItem>(Object.FindObjectsByType<ShipItem>(FindObjectsSortMode.None));

        private static bool IsAtDeliveryPoint(ShipItem item, Transform deliveryPoint) {
            Vector3 offset = item.transform.position - deliveryPoint.position;
            offset.y = 0f;
            return offset.magnitude <= DELIVERY_AREA_RADIUS;
        }

        private static Vector3 FlatForward() =>
            Vector3.ProjectOnPlane(Player.transform.forward, Vector3.up).normalized;

        private static Vector3 CounterCenter(ShopKioskInteractable kiosk) {
            foreach (Collider collider in kiosk.GetComponentsInChildren<Collider>()) {
                if (collider.isTrigger == false)
                    return collider.bounds.center;
            }

            throw new InvalidOperationException("The shop kiosk has no solid counter.");
        }

        internal static IEnumerator WaitForCoroutine(Func<bool> condition, string target) {
            float deadline = Time.realtimeSinceStartup + COMMAND_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {COMMAND_TIMEOUT_SECONDS} s waiting for {target}.");

                yield return null;
            }
        }
    }
}
#endif
