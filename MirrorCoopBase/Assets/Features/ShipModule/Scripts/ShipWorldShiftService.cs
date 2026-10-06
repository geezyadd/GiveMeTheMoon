using UnityEngine;

namespace Features.ShipModule.Scripts {
    internal sealed class ShipWorldShiftService : IShipWorldShiftService
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        , IShipWorldShiftDebug
#endif
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly Vector3 _debugShift = new Vector3(40f, 0f, -25f);

#endif
        private readonly ShipRunConfig _config;
        private readonly IShipRunBindingModel _binding;
        private readonly IShipStationPads _pads;
        private readonly IShipRoute _route;

        public ShipWorldShiftService(
            ShipRunConfig config,
            IShipRunBindingModel binding,
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public int WorldShiftCount => _binding.Ship != null ? _binding.Ship.PoseSync.WorldShift.Count : 0;

        public void DebugForceRecenter() {
            ShipBase ship = _binding.Ship;
            if (ship == null)
                return;

            Vector3 position = ship.transform.position;
            Vector3 delta = new Vector3(-position.x, 0f, -position.z);
            if (delta.sqrMagnitude < 1f)
                delta = _debugShift;

            ApplyWorldShift(ship, delta);
        }
#endif

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
