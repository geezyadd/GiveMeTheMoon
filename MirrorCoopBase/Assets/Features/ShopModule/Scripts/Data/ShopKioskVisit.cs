using UnityEngine;

namespace Features.ShopModule.Scripts.Data {
    // The kiosk a player opened the shop at, as the server saw it.
    public readonly struct ShopKioskVisit {
        public ShopKioskVisit(Transform kiosk, Transform deliveryPoint) {
            Kiosk = kiosk;
            DeliveryPoint = deliveryPoint;
        }

        // Null once the pad of the kiosk is destroyed.
        public Transform Kiosk { get; }
        public Transform DeliveryPoint { get; }
    }
}
