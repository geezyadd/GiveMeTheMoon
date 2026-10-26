using Features.ShipModule.Scripts;
using Features.WeaponModule.Scripts.Feel;
using UnityEngine;

namespace Features.TurretModule.Scripts {
    // The ship's deck as the floor a weapon's casings land on; past the deck edge they fall.
    public sealed class ShipDeckFeelFloor : IWeaponFeelFloor {
        private const float EDGE_INSET = 0f;

        private readonly IShipDeckGeometry _deckGeometry;

        public Transform Frame { get; }

        public ShipDeckFeelFloor(Transform ship, IShipDeckGeometry deckGeometry) {
            Frame = ship;
            _deckGeometry = deckGeometry;
        }

        public bool TryGetFloorHeight(Vector3 framePoint, out float floorHeight) {
            floorHeight = 0f;
            return _deckGeometry.ContainsDeckWalk(framePoint, EDGE_INSET) && _deckGeometry.TryGetDeckSurfaceY(framePoint, out floorHeight);
        }
    }
}
