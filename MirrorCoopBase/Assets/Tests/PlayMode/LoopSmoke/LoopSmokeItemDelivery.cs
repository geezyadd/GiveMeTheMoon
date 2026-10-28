#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Network;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // Test setup on the server: the start station has no free items, so a check that plays loose items first gets
    // them delivered at the kiosk of the pad under the ship, by the shop's own spawn, without paying. The shop
    // itself is checked by LoopSmokeShopCheck and the economy tests.
    public static class LoopSmokeItemDelivery {
        private const float SETTLE_SECONDS = 1.5f;

        // Delivers `count` module items, the catalog's box-shaped modules in turn (the deck checks play items by their
        // box collider), and waits until they land.
        public static IEnumerator DeliverModulesCoroutine(ShipBase ship, int count) =>
            DeliverModulesCoroutine(ship, count, null);

        // The same; `delivered` gets the new items.
        public static IEnumerator DeliverModulesCoroutine(ShipBase ship, int count, Action<Grabbable[]> delivered) {
            HashSet<Grabbable> before = new HashSet<Grabbable>(Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None));
            ShopCatalog catalog = SessionQueries.Resolve<ShopCatalog>();
            List<Grabbable> prefabs = new List<Grabbable>();
            foreach (ModuleDefinition module in catalog.Modules) {
                if (module.ItemPrefab != null && module.ItemPrefab.GetComponentInChildren<BoxCollider>() != null)
                    prefabs.Add(module.ItemPrefab.GetComponent<Grabbable>());
            }

            Assert.IsNotEmpty(prefabs, "The shop catalog has no box-shaped module to deliver.");
            IShopItemSpawnService spawn = SessionQueries.Resolve<IShopItemSpawnService>();
            Transform deliveryPoint = FindKioskNear(ship).DeliveryPoint;
            for (int i = 0; i < count; i++) {
                spawn.SpawnAtDelivery(prefabs[i % prefabs.Count], deliveryPoint);
            }

            yield return new WaitForSeconds(SETTLE_SECONDS);
            List<Grabbable> items = new List<Grabbable>();
            foreach (Grabbable item in Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None)) {
                if (before.Contains(item) == false)
                    items.Add(item);
            }

            Assert.AreEqual(count, items.Count, "Not every delivered item turned up at the kiosk.");
            delivered?.Invoke(items.ToArray());
        }

        private static ShopKioskInteractable FindKioskNear(ShipBase ship) {
            ShopKioskInteractable closest = null;
            float best = float.MaxValue;
            foreach (ShopKioskInteractable kiosk in Object.FindObjectsByType<ShopKioskInteractable>(FindObjectsSortMode.None)) {
                float distance = (kiosk.transform.position - ship.transform.position).sqrMagnitude;
                if (distance >= best)
                    continue;

                best = distance;
                closest = kiosk;
            }

            Assert.IsNotNull(closest, "No shop kiosk in the scene.");
            return closest;
        }
    }
}
#endif
