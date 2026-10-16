#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.CharacterMovableModule.Scripts;
using Features.GameCoreModule.Contracts;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Tests.Shared {
    // Stands in for walking aboard: the player is put above a spot of the deck and lands on it under physics.
    public static class ShipDeckBoarding {
        private const float DROP_HEIGHT = 1f;
        private const float FAR_AWAY = 100f;
        private const float EDGE_INSET = 0.4f;

        public static void PutAboveDeck(CharacterMovableBase player, ShipBase ship) {
            Bounds deck = ship.DeckGeometry.Bounds;
            Vector3 target = new Vector3(deck.center.x, deck.max.y + DROP_HEIGHT, deck.center.z);
            Teleport(player, target);
        }

        // The stern end of the deck lies farther from the ship's pivot than the rest of it.
        public static void PutAboveStern(CharacterMovableBase player, ShipBase ship) =>
            PutAbove(player, ship, DeckEdgeSpot(ship, Vector3.back * FAR_AWAY, Vector3.forward), DROP_HEIGHT);

        public static void PutAbove(CharacterMovableBase player, ShipBase ship, Vector3 shipLocalSpot, float dropHeight) =>
            Teleport(player, ship.transform.TransformPoint(shipLocalSpot + Vector3.up * dropHeight));

        // The walkable deck point nearest to a far ship-local point, moved a little inwards, on the deck surface.
        public static Vector3 DeckEdgeSpot(ShipBase ship, Vector3 farLocal, Vector3 inward) {
            ship.DeckGeometry.TryClosestDeckWalk(farLocal, out Vector3 edge, out _, out _);
            Vector3 spot = edge + inward * EDGE_INSET;
            ship.DeckGeometry.TryGetDeckSurfaceY(spot, out float surfaceY);
            spot.y = surfaceY;
            return spot;
        }

        private static void Teleport(CharacterMovableBase player, Vector3 target) {
            Rigidbody body = player.Body;
            body.position = target;
            body.transform.position = target;
            body.StopIfDynamic();
        }
    }
}
#endif
