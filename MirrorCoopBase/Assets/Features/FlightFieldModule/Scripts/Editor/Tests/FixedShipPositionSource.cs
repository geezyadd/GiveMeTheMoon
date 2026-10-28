using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    public sealed class FixedShipPositionSource : IShipPositionSource {
        public Vector3 Position { get; set; }
        public bool HasShip { get; set; } = true;

        public bool TryGetShipPosition(out Vector3 position) {
            position = Position;
            return HasShip;
        }
    }
}
