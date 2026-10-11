using UnityEngine;

namespace Features.ShopModule.Scripts.Configurations {
    [CreateAssetMenu(
        fileName = nameof(ShopKioskConfiguration) + "_Default",
        menuName = "Configurations/ShopModule/" + nameof(ShopKioskConfiguration))]
    public sealed class ShopKioskConfiguration : ScriptableObject {
        [Tooltip("How close to the kiosk a player must stand to open the shop.")]
        [SerializeField, Min(0f)] private float _openDistance = 5f;

        [Tooltip("The open shop window closes once the player is farther than this from the kiosk. Above the open distance so the window does not flicker at the edge.")]
        [SerializeField, Min(0f)] private float _keepOpenDistance = 6f;

        [Tooltip("How close to the kiosk the server accepts a purchase: the keep-open distance plus slack for latency.")]
        [SerializeField, Min(0f)] private float _purchaseDistance = 7f;

        [Tooltip("Radius around the delivery point in which already delivered items are counted.")]
        [SerializeField, Min(0f)] private float _deliveryOccupancyRadius = 1.2f;

        [Tooltip("Each item already lying at the delivery point lifts the next one by this height, so it never spawns inside them.")]
        [SerializeField, Min(0f)] private float _deliveryStackStep = 0.8f;

        [Tooltip("Horizontal scatter of delivered items around the delivery point, so a stack topples to the sides.")]
        [SerializeField, Min(0f)] private float _deliveryScatter = 0.3f;

        public float OpenDistance =>
            _openDistance;

        public float KeepOpenDistance =>
            _keepOpenDistance;

        public float PurchaseDistance =>
            _purchaseDistance;

        public float DeliveryOccupancyRadius =>
            _deliveryOccupancyRadius;

        public float DeliveryStackStep =>
            _deliveryStackStep;

        public float DeliveryScatter =>
            _deliveryScatter;
    }
}
