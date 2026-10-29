using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Editor.Tests {
    public sealed class FixedShipPosition : IShipPositionSource {
        private readonly Vector3 _position;

        public FixedShipPosition(Vector3 position) =>
            _position = position;

        public bool TryGetShipPosition(out Vector3 position) {
            position = _position;
            return true;
        }
    }
}
