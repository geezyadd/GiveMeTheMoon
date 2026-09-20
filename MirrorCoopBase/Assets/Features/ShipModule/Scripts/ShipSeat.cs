using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipSeat : MonoBehaviour {
        [SerializeField] private ShipSeatRole _role = ShipSeatRole.Passenger;
        [SerializeField] private Transform _sitPoint;

        public ShipSeatRole Role => _role;
        public Transform SitPoint => _sitPoint != null ? _sitPoint : transform;
    }
}
