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
using UnityEngine;
using UnityEngine.UI;
using Zenject;
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
        private const float ITEMS_SETTLE_SECONDS = 2.5f;
        private const float COMMAND_TIMEOUT_SECONDS = 3f;
        private const float REJECTED_COMMAND_WAIT_SECONDS = 0.5f;
        private const float DELIVERY_AREA_RADIUS = 2.5f;
        private const float BELOW_DELIVERY_TOLERANCE = 1.2f;
        private const float SETTLED_SPEED = 0.3f;

        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static LocalPlayerInteraction Interaction => Player.GetComponent<LocalPlayerInteraction>();

        public IEnumerator BuyAtKioskAndWalkAwayCoroutine(ShipLandingPad pad) {
            ShopKioskInteractable kiosk = pad.GetComponentInChildren<ShopKioskInteractable>();
            Assert.IsNotNull(kiosk, $"The pad {pad.name} has no shop kiosk.");
            ShopModel shop = Resolve<ShopModel>();
            IReadOnlyWalletModel wallet = Resolve<IReadOnlyWalletModel>();
            List<ShipItem> itemsBefore = FindItemsAt(kiosk.DeliveryPoint);
            try {
                CameraLookDriver.DebugPitchOverride = true;
                yield return OpenAtKioskCoroutine(kiosk, shop);
                yield return BuyCoroutine(wallet);
                yield return WalkAwayCoroutine(kiosk, shop);
                yield return AssertDeliveredCoroutine(kiosk, itemsBefore);
                yield return AssertFarPurchaseRefusedCoroutine(wallet);
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }
        }

        private static IEnumerator OpenAtKioskCoroutine(ShopKioskInteractable kiosk, ShopModel shop) {
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

        private static IEnumerator WalkAwayCoroutine(ShopKioskInteractable kiosk, ShopModel shop) {
            Vector3 away = kiosk.transform.position - FlatForward() * WALK_AWAY_DISTANCE;
            yield return StandAtCoroutine(away + Vector3.up * STAND_HEIGHT);
            Assert.IsFalse(shop.IsOpen, "The shop window stayed open after the player walked away from the kiosk.");
        }

        private static IEnumerator AssertDeliveredCoroutine(ShopKioskInteractable kiosk, List<ShipItem> itemsBefore) {
            yield return new WaitForSeconds(ITEMS_SETTLE_SECONDS);
            List<ShipItem> delivered = FindItemsAt(kiosk.DeliveryPoint);
            delivered.RemoveAll(itemsBefore.Contains);
            Assert.AreEqual(PURCHASES, delivered.Count, "Not every bought item lies at the kiosk's delivery point.");

            float floor = kiosk.transform.position.y - BELOW_DELIVERY_TOLERANCE;
            foreach (ShipItem item in delivered) {
                Rigidbody body = item.GetComponent<Rigidbody>();
                Assert.Greater(item.transform.position.y, floor, $"{item.name} fell through the pad.");
                Assert.Less(body.linearVelocity.magnitude, SETTLED_SPEED, $"{item.name} does not rest: stuck in another item?");
                Assert.IsTrue(item.GetComponent<Grabbable>().CanBeGrabbed, $"{item.name} cannot be picked up.");
            }
        }

        private static IEnumerator AssertFarPurchaseRefusedCoroutine(IReadOnlyWalletModel wallet) {
            long balance = wallet.Balance;
            Resolve<ShopPurchaseRequestEventClass>().InvokePurchaseRequested(BOUGHT_ENTRY);
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
            ShopItemRow[] rows = Object.FindObjectsByType<ShopItemRow>(FindObjectsSortMode.InstanceID);
            List<ShopItemRow> active = new List<ShopItemRow>();
            foreach (ShopItemRow row in rows) {
                if (row.isActiveAndEnabled)
                    active.Add(row);
            }

            active.Sort((left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
            Assert.Greater(active.Count, entry, "The shop window shows too few rows.");
            return active[entry].GetComponentInChildren<Button>();
        }

        private static List<ShipItem> FindItemsAt(Transform deliveryPoint) {
            List<ShipItem> items = new List<ShipItem>();
            foreach (ShipItem item in Object.FindObjectsByType<ShipItem>(FindObjectsSortMode.None)) {
                Vector3 offset = item.transform.position - deliveryPoint.position;
                offset.y = 0f;
                if (offset.magnitude <= DELIVERY_AREA_RADIUS)
                    items.Add(item);
            }

            return items;
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

        private static T Resolve<T>() {
            foreach (SceneContext context in Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None)) {
                if (context.Container.HasBinding<T>())
                    return context.Container.Resolve<T>();
            }

            throw new InvalidOperationException($"No scene context binds {typeof(T).Name}.");
        }

        private static IEnumerator WaitForCoroutine(Func<bool> condition, string target) {
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
