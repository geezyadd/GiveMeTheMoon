using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Moves the world origin back under the ship: the ship, the pads, the route destination and the director's world.
    internal sealed class ShipWorldShiftService : IShipWorldShiftService {
        private readonly ShipRunConfig _config;
        private readonly IShipRunBinding _binding;
        private readonly IShipStationPads _pads;
        private readonly IShipRoute _route;

        public int WorldShiftCount => _binding.Ship != null ? _binding.Ship.PoseSync.WorldShift.Count : 0;

        public ShipWorldShiftService(
            ShipRunConfig config,
            IShipRunBinding binding,
            IShipStationPads pads,
            IShipRoute route) {
            _config = config;
            _binding = binding;
            _pads = pads;
            _route = route;
        }

        public void RecenterIfFar() {
            ShipBase ship = _binding.Ship;
            if (ship == null)
                return;

            Vector3 position = ship.transform.position;
            float limit = _config.OriginRecenterDistance;
            if (position.x * position.x + position.z * position.z < limit * limit)
                return;

            ApplyWorldShift(ship, new Vector3(-position.x, 0f, -position.z));
        }

        public void DebugForceRecenter() {
            ShipBase ship = _binding.Ship;
            if (ship == null)
                return;

            Vector3 position = ship.transform.position;
            Vector3 delta = new Vector3(-position.x, 0f, -position.z);
            if (delta.sqrMagnitude < 1f)
                delta = new Vector3(40f, 0f, -25f);

            ApplyWorldShift(ship, delta);
        }

        private void ApplyWorldShift(ShipBase ship, Vector3 delta) {
            ship.ServerApplyWorldShift(delta);
            _pads.Shift(delta);
            _route.ShiftDestination(delta);
            ShipRunDirector director = _binding.Director;
            if (director != null)
                director.ServerShiftWorld(delta, _pads.CurrentIdentity, _pads.PreviousIdentity);
        }
    }
}
