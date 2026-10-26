using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipSeat : MonoBehaviour {
        [SerializeField] private Transform _sitPoint;

        public Transform SitPoint => _sitPoint != null ? _sitPoint : transform;
    }
}
