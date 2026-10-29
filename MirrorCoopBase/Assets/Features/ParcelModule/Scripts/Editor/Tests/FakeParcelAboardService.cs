using Features.ParcelModule.Scripts.Services;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Editor.Tests {
    // Says whether every crate is on board.
    public sealed class FakeParcelAboardService : IParcelAboardService {
        public bool IsEverythingAboard { get; set; } = true;

        public bool IsAboard(Vector3 worldPosition, ShipBase ship) =>
            IsEverythingAboard;

        public float GetHeightAboveDeck(Vector3 worldPosition, ShipBase ship) =>
            0f;

        public bool IsFlownInto(Vector3 worldPosition, ShipBase ship) =>
            false;

        public Vector3 CalculateDeckDropPoint(Vector3 worldPosition, ShipBase ship) =>
            worldPosition;
    }
}
