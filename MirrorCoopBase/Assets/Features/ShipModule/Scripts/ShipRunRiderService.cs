using Features.ShipModule.Scripts.Generated;
using Features.ShipModule.Scripts.Overboard;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    internal sealed class ShipRunRiderService : IShipFloorReferenceProvider, IShipRiderRelease {
        private readonly IReadOnlyShipRunModel _model;
        private readonly IShipRunBindingModel _binding;
        private readonly IShipStationPads _pads;

        public ShipRunRiderService(IReadOnlyShipRunModel model, IShipRunBindingModel binding, IShipStationPads pads) {
            _model = model;
            _binding = binding;
            _pads = pads;
        }

        public bool TryGetWalkableFloorY(out bool isFlying, out float floorY) {
            isFlying = false;
            floorY = 0f;
            if (_model.Phase.IsFlight()) {
                ShipBase ship = _binding.Ship;
                if (ship == null || TryGetDeckSurfaceWorldY(ship, out floorY) == false)
                    return false;

                isFlying = true;
                return true;
            }

            ShipLandingPad currentPad = _pads.CurrentPad;
            if (currentPad == null)
                return false;

            floorY = currentPad.BuildBerth.position.y;
            return true;
        }

        public void ServerReleaseRider(NetworkIdentity player) {
            if (NetworkServer.active == false)
                throw new System.InvalidOperationException("ServerReleaseRider can only be called on the server.");

            // No ship outside a run (lobby): there is nothing to release.
            ShipBase ship = _binding.Ship;
            if (ship == null)
                return;

            if (player.TryGetComponent(out ShipRider rider) == false)
                throw new System.InvalidOperationException(player.name + " has no " + nameof(ShipRider) + ".");

            if (player.TryGetComponent(out ShipOverboardRider overboardRider))
                overboardRider.ServerReset();

            ship.Seats.ServerStand(rider);
            ship.Riders.UnregisterRider(rider);
            ship.Throttle.ServerLetGo(rider.Key.Id);
        }

        private static bool TryGetDeckSurfaceWorldY(ShipBase ship, out float worldY) {
            worldY = 0f;
            if (ship.DeckGeometry.TryGetDeckSurfaceY(Vector3.zero, out float surfaceLocalY) == false)
                return false;

            worldY = ship.transform.TransformPoint(new Vector3(0f, surfaceLocalY, 0f)).y;
            return true;
        }
    }
}
