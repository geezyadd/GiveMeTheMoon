using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    internal sealed class ShipStationPads : IShipStationPads {
        private readonly ShipStationCatalog _stations;
        private readonly ShipRunConfig _config;
        private readonly IShipRunBindingModel _binding;

        private ShipLandingPad _previousPad;

        public ShipLandingPad CurrentPad { get; private set; }
        public NetworkIdentity CurrentIdentity => PadIdentity(CurrentPad);
        public NetworkIdentity PreviousIdentity => PadIdentity(_previousPad);

        public ShipStationPads(ShipStationCatalog stations, ShipRunConfig config, IShipRunBindingModel binding) {
            _stations = stations;
            _config = config;
            _binding = binding;
        }

        public void Reset(ShipLandingPad startPad) {
            CurrentPad = startPad;
            _previousPad = null;
        }

        // Without a pad prefab no pad spawns: the pad stays the current one and the result is false.
        public bool TrySpawnNext(Vector3 padPosition, Vector3 forward, bool matchLandingPoint, out ShipLandingPad pad) {
            pad = CurrentPad;
            GameObject prefab = _stations.PadPrefab;
            if (prefab == null)
                return false;

            ShipBase ship = _binding.Ship;
            Vector3 origin = ship != null ? ship.transform.position : padPosition;
            if (matchLandingPoint == false) {
                float padY = CurrentPad != null ? CurrentPad.transform.position.y : origin.y;
                padPosition.y = padY + _config.TakeoffHeight;
            }

            Quaternion padRotation = Quaternion.LookRotation(forward, Vector3.up);
            GameObject instance = Object.Instantiate(prefab, padPosition, padRotation);
            pad = instance.GetComponentInChildren<ShipLandingPad>();
            // Pads have no transform sync: clients only get the spawn pose, so the pad is placed before the spawn.
            if (matchLandingPoint)
                instance.transform.position += padPosition - pad.LandingPoint.position;

            NetworkServer.Spawn(instance);

            if (_previousPad != null)
                NetworkServer.Destroy(_previousPad.gameObject);

            _previousPad = CurrentPad;
            CurrentPad = pad;
            return true;
        }

        public void Shift(Vector3 delta) {
            ShiftPad(CurrentPad, delta);
            ShiftPad(_previousPad, delta);
        }

        private static void ShiftPad(ShipLandingPad pad, Vector3 delta) {
            if (pad != null)
                pad.transform.position += delta;
        }

        private static NetworkIdentity PadIdentity(ShipLandingPad pad) =>
            pad != null ? pad.GetComponent<NetworkIdentity>() : null;
    }
}
