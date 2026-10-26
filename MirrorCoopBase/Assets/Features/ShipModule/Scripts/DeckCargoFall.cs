using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The flying deck is moved by code, so physics cannot carry a loose item on it, and an item pinned where it left the
    // hand would hang in the air: an item the deck takes falls here, in ship space, onto the deck or what lies on it.
    internal sealed class DeckCargoFall {
        private const float SWEEP_SKIN = 0.01f;
        private const float REST_GAP = 0.005f;

        private readonly IShipDeckGeometry _deckGeometry;
        private readonly DeckCargoLowestPoint _lowestPoint;
        private readonly Transform _ship;

        internal DeckCargoFall(Transform ship, IShipDeckGeometry deckGeometry) {
            _ship = ship;
            _deckGeometry = deckGeometry;
            _lowestPoint = new DeckCargoLowestPoint(ship);
        }

        // The item has to be pinned at its ship-space pose: the gap and the sweep are measured from where it is now.
        public void Step(DeckCargoItem item, float deltaTime) {
            if (item.IsResting)
                return;

            float gapToDeck = MeasureGapToDeck(item);
            float fallSpeed = item.FallSpeed + Physics.gravity.magnitude * deltaTime;
            float drop = Mathf.Min(fallSpeed * deltaTime, gapToDeck);
            bool landed = drop >= gapToDeck - REST_GAP;
            if (TrySweepDown(item, drop, out float freeDrop)) {
                drop = freeDrop;
                landed = true;
            }

            if (landed)
                item.Rest(item.LocalPosition - Vector3.up * drop);
            else
                item.Fall(drop, fallSpeed);
        }

        // The deck surface stops the fall even where the sweep misses it (a collider it already touches).
        private float MeasureGapToDeck(DeckCargoItem item) {
            if (_lowestPoint.TryFindLowestShipY(item.Colliders, out float lowestY) == false)
                return 0f;

            if (_deckGeometry.TryGetDeckSurfaceY(item.LocalPosition, out float surfaceY) == false)
                return 0f;

            return Mathf.Max(0f, lowestY - surfaceY);
        }

        // Whatever lies on the deck (a module, another item) stops the fall before the deck does.
        private bool TrySweepDown(DeckCargoItem item, float drop, out float freeDrop) {
            freeDrop = drop;
            if (item.Rigidbody == null || drop <= 0f)
                return false;

            if (item.Rigidbody.SweepTest(-_ship.up, out RaycastHit hit, drop + SWEEP_SKIN, QueryTriggerInteraction.Ignore) == false)
                return false;

            freeDrop = Mathf.Clamp(hit.distance - SWEEP_SKIN, 0f, drop);
            return true;
        }
    }
}
