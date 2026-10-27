#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;
using Features.ShopModule.Scripts.Network;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // Buys one shop entry at the kiosk of the pad under the ship the way a player does (aim, interact, the window's
    // Buy button, walk away) and picks the delivered item up by hand, for the checks that need a bought item. A run
    // that cannot afford the entry gets money from the debug window first, so the check does not depend on income.
    public static class LoopSmokeKiosk {
        private const float WALK_AWAY_DISTANCE = 8f;
        private const float STAND_HEIGHT = 0.8f;
        private const float STAND_SETTLE_SECONDS = 1f;
        private const float DELIVERY_TIMEOUT_SECONDS = 4f;
        private const float DELIVERY_SETTLE_SECONDS = 1.5f;
        private const float ITEM_APPROACH_DISTANCE = 1.6f;

        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static LocalPlayerInteraction Interaction => Player.GetComponent<LocalPlayerInteraction>();
        private static GrabController Hand => Player.GetComponent<GrabController>();

        public static int FindModuleEntry(string moduleId) {
            ShopCatalog catalog = SessionQueries.Resolve<ShopCatalog>();
            for (int i = 0; i < catalog.Modules.Count; i++) {
                if (catalog.Modules[i].Id == moduleId)
                    return i;
            }

            throw new InvalidOperationException($"The shop does not sell the module {moduleId}.");
        }

        public static int FindHandWeaponEntry(string handWeaponId) {
            ShopCatalog catalog = SessionQueries.Resolve<ShopCatalog>();
            for (int i = 0; i < catalog.HandWeapons.Count; i++) {
                if (catalog.HandWeapons[i].Id == handWeaponId)
                    return catalog.Modules.Count + catalog.Tools.Count + i;
            }

            throw new InvalidOperationException($"The shop does not sell the hand weapon {handWeaponId}.");
        }

        // Opens the kiosk, buys the entry once and walks away; `bought` gets the delivered item.
        public static IEnumerator BuyCoroutine<TItem>(LoopSmokeFlight flight, int entry, long price, Func<TItem, bool> isBought, Action<TItem> bought) where TItem : Component {
            ShopKioskInteractable kiosk = flight.FindPadUnderShip().GetComponentInChildren<ShopKioskInteractable>();
            ShopModel shop = SessionQueries.Resolve<ShopModel>();
            IReadOnlyWalletModel wallet = SessionQueries.Resolve<IReadOnlyWalletModel>();
            yield return AffordCoroutine(wallet, price);

            yield return LoopSmokeShopCheck.OpenAtKioskCoroutine(kiosk, shop);
            long balance = wallet.Balance;
            LoopSmokeShopCheck.FindBuyButton(entry).onClick.Invoke();
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => wallet.Balance == balance - price, $"the balance to drop by {price} after buying entry {entry}");

            yield return StandOnPadCoroutine(flight);
            Assert.IsFalse(shop.IsOpen, "The shop window stayed open after the player walked away from the kiosk.");

            TItem item = null;
            yield return WaitForCoroutine(() => TryFindItem(isBought, out item), $"the bought entry {entry} delivered at the kiosk");
            yield return new WaitForSeconds(DELIVERY_SETTLE_SECONDS);
            bought(item);
        }

        // Stands in front of the item on its own floor, aims at it and grabs it.
        public static IEnumerator PickUpCoroutine(Grabbable item) {
            Vector3 itemPosition = item.transform.position;
            yield return StandAtCoroutine(itemPosition - FlatForward() * ITEM_APPROACH_DISTANCE + Vector3.up * STAND_HEIGHT);
            yield return LoopSmokeHandCheck.AimAtCoroutine(item.GetComponentInChildren<Collider>().bounds.center);
            Assert.AreSame(item, Interaction.Target.Grabbable, $"The bought {item.name} is not under the crosshair.");
            Interaction.Interact();
            yield return WaitForCoroutine(() => Hand.Held == item, $"the bought {item.name} in the hand");
        }

        // On the pad of the kiosk under the ship, away from the counter and its delivery point.
        public static IEnumerator StandOnPadCoroutine(LoopSmokeFlight flight) {
            ShopKioskInteractable kiosk = flight.FindPadUnderShip().GetComponentInChildren<ShopKioskInteractable>();
            yield return StandAtCoroutine(kiosk.transform.position - FlatForward() * WALK_AWAY_DISTANCE + Vector3.up * STAND_HEIGHT);
        }

        public static IEnumerator StandAtCoroutine(Vector3 position) {
            Rigidbody body = Player.GetComponent<CharacterMovableBase>().Body;
            body.position = position;
            body.transform.position = position;
            body.linearVelocity = Vector3.zero;
            yield return new WaitForSeconds(STAND_SETTLE_SECONDS);
        }

        // Tops the crew wallet up through the debug window's money action until the purchase is affordable.
        private static IEnumerator AffordCoroutine(IReadOnlyWalletModel wallet, long price) {
            while (wallet.Balance < price)
                yield return LoopSmokeDebugMoneyGrant.GrantThroughDebugWindowCoroutine();
        }

        private static bool TryFindItem<TItem>(Func<TItem, bool> isBought, out TItem item) where TItem : Component {
            item = null;
            foreach (TItem candidate in UnityEngine.Object.FindObjectsByType<TItem>(FindObjectsSortMode.None)) {
                if (isBought(candidate) == false)
                    continue;

                item = candidate;
                return true;
            }

            return false;
        }

        private static Vector3 FlatForward() =>
            Vector3.ProjectOnPlane(Player.transform.forward, Vector3.up).normalized;

        private static IEnumerator WaitForCoroutine(Func<bool> condition, string target) {
            float deadline = Time.realtimeSinceStartup + DELIVERY_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {DELIVERY_TIMEOUT_SECONDS} s waiting for {target}.");

                yield return null;
            }
        }
    }
}
#endif
