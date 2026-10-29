using UnityEngine;

namespace Features.ShipModule.Scripts.Overboard {
    // When a player is overboard, when the ship takes it back and when the void takes it. The ship's volume is the
    // deck's walk area widened by an edge and a band of heights over the deck surface (measured from the surface,
    // not the ship's pivot). Only a player who rode the ship in this flight goes overboard: one left behind at a
    // station stands on the station's ground as before, and nobody goes overboard while the ship stands at one.
    public sealed class ShipOverboardRule : IShipOverboardRule {
        private readonly ShipOverboardConfiguration _shipOverboardConfiguration;

        public ShipOverboardRule(ShipOverboardConfiguration shipOverboardConfiguration) =>
            _shipOverboardConfiguration = shipOverboardConfiguration;

        public bool IsInShipVolume(IShipDeckGeometry deckGeometry, Vector3 shipLocal) {
            if (deckGeometry.TryClosestDeckWalk(shipLocal, out Vector3 closestLocal, out float dx, out float dz) == false)
                return false;

            float edge = _shipOverboardConfiguration.VolumeEdgeMeters;
            bool isBesideDeck = dx * dx + dz * dz <= edge * edge;
            if (isBesideDeck == false || deckGeometry.TryGetDeckSurfaceY(closestLocal, out float surfaceY) == false)
                return false;

            float height = shipLocal.y - surfaceY;
            return height >= _shipOverboardConfiguration.VolumeMinHeightMeters && height <= _shipOverboardConfiguration.VolumeMaxHeightMeters;
        }

        public ShipOverboardDecision Decide(in ShipOverboardState state) =>
            state.IsOverboard ? DecideDrifting(state) : DecideAboard(state);

        public bool IsRopeHeld(bool hasHolderWeapon, float holderDistanceMeters, float maxRopeMeters) =>
            hasHolderWeapon && holderDistanceMeters <= maxRopeMeters;

        private static ShipOverboardDecision DecideAboard(in ShipOverboardState state) {
            if (state.IsFlying == false)
                return state.HasRiddenThisFlight ? ShipOverboardDecision.ForgetRide : ShipOverboardDecision.None;

            if (state.IsRiding)
                return state.HasRiddenThisFlight ? ShipOverboardDecision.None : ShipOverboardDecision.MarkRidden;

            bool hasLeftShip = state.HasRiddenThisFlight && state.IsInShipVolume == false;
            return hasLeftShip ? ShipOverboardDecision.GoOverboard : ShipOverboardDecision.None;
        }

        private ShipOverboardDecision DecideDrifting(in ShipOverboardState state) {
            // The deck's own catch bound the player (it came down on the deck): the ride path took it back already.
            if (state.IsRiding)
                return ShipOverboardDecision.EndOnDeck;

            if (state.IsInShipVolume && state.DriftSeconds >= _shipOverboardConfiguration.MinDriftSecondsBeforeCapture)
                return ShipOverboardDecision.Capture;

            bool isPastLimit = state.DistanceMeters > _shipOverboardConfiguration.MaxDistanceMeters || state.DriftSeconds > _shipOverboardConfiguration.MaxDriftSeconds;
            return isPastLimit ? ShipOverboardDecision.Expire : ShipOverboardDecision.None;
        }
    }
}
