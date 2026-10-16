using System;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Whether a rider at a ship-local point may be bound to the deck: when the ship takes off (boarding) and when the
    // rider comes down on the deck in flight (catch). Heights are measured from the deck surface under the rider, not
    // from the ship's pivot, so a deck above or below the pivot or a rider on a raised spot is judged the same way.
    public sealed class ShipDeckRideRule {
        private readonly IShipDeckGeometry _deckGeometry;
        // Read on use: the settings can be injected after the ship's parts are created.
        private readonly Func<ShipFlightSettings> _flightSettings;

        public ShipDeckRideRule(IShipDeckGeometry deckGeometry, Func<ShipFlightSettings> flightSettings) {
            _deckGeometry = deckGeometry;
            _flightSettings = flightSettings;
        }

        public ShipRideRefusal CheckBoarding(Vector3 shipLocal) {
            ShipFlightSettings flightSettings = _flightSettings();
            if (IsWithinDeckEdge(shipLocal, flightSettings.BoardingEdgeTolerance) == false)
                return ShipRideRefusal.OffDeck;

            return CheckHeight(shipLocal, flightSettings.BoardingMinHeight, flightSettings.BoardingMaxHeight);
        }

        public ShipRideRefusal CheckCatch(Vector3 shipLocal, float verticalSpeed, float deckInset) {
            ShipFlightSettings flightSettings = _flightSettings();
            if (_deckGeometry.ContainsDeckWalk(shipLocal, deckInset) == false)
                return ShipRideRefusal.OffDeck;

            if (verticalSpeed > flightSettings.CatchMaxRiseSpeed)
                return ShipRideRefusal.Rising;

            return CheckHeight(shipLocal, flightSettings.CatchMinHeight, flightSettings.CatchMaxHeight);
        }

        public bool TryGetHeightAboveDeck(Vector3 shipLocal, out float height) {
            height = 0f;
            if (_deckGeometry.TryGetDeckSurfaceY(shipLocal, out float surfaceY) == false)
                return false;

            height = shipLocal.y - surfaceY;
            return true;
        }

        private bool IsWithinDeckEdge(Vector3 shipLocal, float edgeTolerance) =>
            _deckGeometry.TryClosestDeckWalk(shipLocal, out _, out float dx, out float dz)
            && dx * dx + dz * dz <= edgeTolerance * edgeTolerance;

        private ShipRideRefusal CheckHeight(Vector3 shipLocal, float minHeight, float maxHeight) {
            if (TryGetHeightAboveDeck(shipLocal, out float height) == false)
                return ShipRideRefusal.OffDeck;

            if (height < minHeight)
                return ShipRideRefusal.BelowWindow;

            return height > maxHeight ? ShipRideRefusal.AboveWindow : ShipRideRefusal.None;
        }
    }
}
