using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipDeckRideVolume : MonoBehaviour {
        [SerializeField] private ShipBase _ship;

        private void OnTriggerEnter(Collider other) {
            if (_ship == null || other == null)
                return;

            ShipRider rider = other.GetComponentInParent<ShipRider>();
            if (rider != null)
                _ship.SetVolumeOverlap(rider, true);
        }

        private void OnTriggerExit(Collider other) {
            if (_ship == null || other == null)
                return;

            ShipRider rider = other.GetComponentInParent<ShipRider>();
            if (rider != null)
                _ship.SetVolumeOverlap(rider, false);
        }
    }
}
