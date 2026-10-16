#if UNITY_INCLUDE_TESTS
using Features.CharacterMovableModule.Scripts;
using Features.ShipModule.Scripts;
using Mirror;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.ShipRide {
    // Spots on the ship's walkable deck, found from the deck geometry: the centre, the nose, the stern and both side
    // edges.
    public sealed class ShipRideDeckSpots {
        private const float FAR_AWAY = 100f;
        private const float DROP_HEIGHT = 0.5f;
        // How far a player may stand off the walkable deck and still count as on it.
        private const float ON_DECK_TOLERANCE = 0.6f;
        // A player this far below the deck surface fell off.
        private const float BELOW_DECK_TOLERANCE = 1f;

        private readonly ShipBase _ship;

        public ShipRideDeckSpots(ShipBase ship) =>
            _ship = ship;

        public Vector3 Centre {
            get {
                _ship.DeckGeometry.TryGetDeckStandPoint(out Vector3 stand);
                _ship.DeckGeometry.TryGetDeckSurfaceY(stand, out float surfaceY);
                stand.y = surfaceY;
                return stand;
            }
        }

        public Vector3 Nose =>
            ShipDeckBoarding.DeckEdgeSpot(_ship, Vector3.forward * FAR_AWAY, Vector3.back);

        public Vector3 Stern =>
            ShipDeckBoarding.DeckEdgeSpot(_ship, Vector3.back * FAR_AWAY, Vector3.forward);

        public Vector3 PortSide =>
            ShipDeckBoarding.DeckEdgeSpot(_ship, new Vector3(-FAR_AWAY, 0f, Stern.z * 0.5f), Vector3.right);

        public Vector3 StarboardSide =>
            ShipDeckBoarding.DeckEdgeSpot(_ship, new Vector3(FAR_AWAY, 0f, 0f), Vector3.left);

        public void PutPlayerAt(NetworkIdentity player, Vector3 shipLocalSpot) =>
            ShipDeckBoarding.PutAbove(player.GetComponent<CharacterMovableBase>(), _ship, shipLocalSpot, DROP_HEIGHT);

        public bool IsOnDeck(Vector3 worldPosition) {
            Vector3 local = _ship.transform.InverseTransformPoint(worldPosition);
            if (_ship.DeckGeometry.TryClosestDeckWalk(local, out _, out float dx, out float dz) == false)
                return false;

            if (dx * dx + dz * dz > ON_DECK_TOLERANCE * ON_DECK_TOLERANCE)
                return false;

            return HeightAboveDeck(worldPosition) > -BELOW_DECK_TOLERANCE;
        }

        public float HeightAboveDeck(Vector3 worldPosition) {
            Vector3 local = _ship.transform.InverseTransformPoint(worldPosition);
            _ship.DeckGeometry.TryGetDeckSurfaceY(local, out float surfaceY);
            return local.y - surfaceY;
        }

        public string Describe(Vector3 worldPosition) =>
            $"ship-local {_ship.transform.InverseTransformPoint(worldPosition)}, {HeightAboveDeck(worldPosition):0.00} m above the deck surface";
    }
}
#endif
