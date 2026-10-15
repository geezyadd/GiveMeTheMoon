#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.CharacterMovableModule.Scripts;
using Features.GameCoreModule.Contracts;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Tests.Shared {
    // Stands in for walking aboard: the player is put above the deck centre and lands on it under physics.
    public static class ShipDeckBoarding {
        private const float DROP_HEIGHT = 1f;

        public static void PutAboveDeck(CharacterMovableBase player, ShipBase ship) {
            Bounds deck = ship.DeckGeometry.Bounds;
            Vector3 target = new Vector3(deck.center.x, deck.max.y + DROP_HEIGHT, deck.center.z);
            Rigidbody body = player.Body;
            body.position = target;
            body.transform.position = target;
            body.StopIfDynamic();
        }
    }
}
#endif
