using Features.ParcelModule.Scripts.Configuration;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    public sealed class ParcelAboardService : IParcelAboardService {
        private readonly ParcelConfiguration _parcelConfiguration;

        public ParcelAboardService(ParcelConfiguration parcelConfiguration) =>
            _parcelConfiguration = parcelConfiguration;

        public bool IsAboard(Vector3 worldPosition, ShipBase ship) {
            Vector3 local = ship.transform.InverseTransformPoint(worldPosition);
            if (ship.DeckGeometry.ContainsDeckWalk(local, 0f) == false || ship.DeckGeometry.TryGetDeckSurfaceY(local, out float surfaceY) == false)
                return false;

            return IsWithinHeight(local.y - surfaceY, _parcelConfiguration.AboardBelowMeters);
        }

        public float GetHeightAboveDeck(Vector3 worldPosition, ShipBase ship) =>
            ship.transform.InverseTransformPoint(worldPosition).y;

        public bool IsFlownInto(Vector3 worldPosition, ShipBase ship) {
            Vector3 local = ship.transform.InverseTransformPoint(worldPosition);
            Vector3 onDeck = local;
            ship.DeckGeometry.ClampDeckWalk(ref onDeck, 0f);
            Vector2 outside = new(local.x - onDeck.x, local.z - onDeck.z);
            if (outside.magnitude > _parcelConfiguration.FlyIntoMarginMeters || ship.DeckGeometry.TryGetDeckSurfaceY(onDeck, out float surfaceY) == false)
                return false;

            return IsWithinHeight(local.y - surfaceY, _parcelConfiguration.FlyIntoBelowMeters);
        }

        public Vector3 CalculateDeckDropPoint(Vector3 worldPosition, ShipBase ship) {
            Vector3 local = ship.transform.InverseTransformPoint(worldPosition);
            ship.DeckGeometry.ClampDeckWalk(ref local, 0f);
            if (ship.DeckGeometry.TryGetDeckSurfaceY(local, out float surfaceY))
                local.y = surfaceY + _parcelConfiguration.DeckDropHeightMeters;

            return ship.transform.TransformPoint(local);
        }

        private bool IsWithinHeight(float heightOverSurface, float belowMeters) =>
            heightOverSurface >= -belowMeters && heightOverSurface <= _parcelConfiguration.AboardMaxHeightMeters;
    }
}
