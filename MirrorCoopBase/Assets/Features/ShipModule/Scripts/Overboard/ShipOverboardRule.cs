using UnityEngine;

namespace Features.ShipModule.Scripts.Overboard {
    // When a player is overboard, when it is back and what the void does. Two places a player can drift away from:
    // - In flight, the flying ship: its volume is the deck's walk area widened by an edge and a band of heights over
    //   the deck surface (measured from the surface, not the ship's pivot). Only a player who rode the ship in this
    //   flight goes overboard. The ship takes a drifter back on its owner's peer (ShipOverboardRider): the drift ends
    //   once the server sees it aboard again; past the limit the void takes it (a death).
    // - On the ground (the lobby, a ship standing at a station), the overboard areas and the standing ship's volume:
    //   an area is a floor's bounds widened by the same edge and band. A player who was inside and leaves drifts;
    //   right over a floor again after a moment, the drift ends and it falls back onto the floor; past the area limit
    //   it is put back at the area's return point.
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

            return IsInHeightBand(shipLocal.y - surfaceY);
        }

        // Beside the floor's bounds (within the edge, horizontally) and in the band of heights over its top.
        public bool IsInArea(Bounds floor, Vector3 point) {
            float dx = Mathf.Max(floor.min.x - point.x, 0f, point.x - floor.max.x);
            float dz = Mathf.Max(floor.min.z - point.z, 0f, point.z - floor.max.z);
            float edge = _shipOverboardConfiguration.VolumeEdgeMeters;
            return dx * dx + dz * dz <= edge * edge && IsInHeightBand(point.y - floor.max.y);
        }

        // Right over the floor (inside its bounds, horizontally) and above its top, within the band: where a drifter
        // can let gravity back without falling past the floor's edge.
        public bool IsOverFloor(Bounds floor, Vector3 point) {
            bool isOver = point.x >= floor.min.x && point.x <= floor.max.x && point.z >= floor.min.z && point.z <= floor.max.z;
            float height = point.y - floor.max.y;
            return isOver && height >= 0f && height <= _shipOverboardConfiguration.VolumeMaxHeightMeters;
        }

        // Right over the deck's walk area and above its surface, within the band.
        public bool IsOverShipDeck(IShipDeckGeometry deckGeometry, Vector3 shipLocal) {
            if (deckGeometry.ContainsDeckWalk(shipLocal, 0f) == false || deckGeometry.TryGetDeckSurfaceY(shipLocal, out float surfaceY) == false)
                return false;

            float height = shipLocal.y - surfaceY;
            return height >= 0f && height <= _shipOverboardConfiguration.VolumeMaxHeightMeters;
        }

        public ShipOverboardDecision Decide(in ShipOverboardState state) {
            if (state.IsFlying)
                return state.IsOverboard ? DecideDriftingInFlight(state) : DecideAboardInFlight(state);

            return state.IsOverboard ? DecideDriftingOnGround(state) : DecideOnGround(state);
        }

        // A loose item drifts like a player once it is outside every safe space (the ship's volume, any overboard
        // area), in flight and on the ground alike, so one thrown off the flying ship hangs where it was and falls
        // behind. Right over a floor again after a moment, it gets gravity back; too far from all of them, it is gone.
        public ShipOverboardItemDecision DecideItem(in ShipOverboardItemState state) {
            if (state.IsDrifting == false)
                return state.IsInSafeSpace ? ShipOverboardItemDecision.None : ShipOverboardItemDecision.BeginDrift;

            if (state.IsOverFloor && state.DriftSeconds >= _shipOverboardConfiguration.MinDriftSecondsBeforeCapture)
                return ShipOverboardItemDecision.EndDrift;

            return state.DistanceMeters > _shipOverboardConfiguration.ItemMaxDistanceMeters
                ? ShipOverboardItemDecision.Remove
                : ShipOverboardItemDecision.None;
        }

        private bool IsInHeightBand(float height) =>
            height >= _shipOverboardConfiguration.VolumeMinHeightMeters && height <= _shipOverboardConfiguration.VolumeMaxHeightMeters;

        private static ShipOverboardDecision DecideAboardInFlight(in ShipOverboardState state) {
            if (state.IsRiding)
                return state.HasBeenInside ? ShipOverboardDecision.None : ShipOverboardDecision.MarkInside;

            bool hasLeftShip = state.HasBeenInside && state.IsInSafeSpace == false;
            return hasLeftShip ? ShipOverboardDecision.GoOverboard : ShipOverboardDecision.None;
        }

        private ShipOverboardDecision DecideDriftingInFlight(in ShipOverboardState state) {
            // The drifting body touched the deck and the ship carries it again (or a seat took it): it is back aboard.
            if (state.IsRiding)
                return ShipOverboardDecision.EndDrift;

            bool isPastLimit = state.DistanceMeters > _shipOverboardConfiguration.MaxDistanceMeters || state.DriftSeconds > _shipOverboardConfiguration.MaxDriftSeconds;
            return isPastLimit ? ShipOverboardDecision.Expire : ShipOverboardDecision.None;
        }

        private static ShipOverboardDecision DecideOnGround(in ShipOverboardState state) {
            if (state.IsInSafeSpace)
                return state.HasBeenInside ? ShipOverboardDecision.None : ShipOverboardDecision.MarkInside;

            return state.HasBeenInside ? ShipOverboardDecision.GoOverboard : ShipOverboardDecision.None;
        }

        private ShipOverboardDecision DecideDriftingOnGround(in ShipOverboardState state) {
            // Only over a floor: beside or under it the gravity coming back would pull the player off again.
            if (state.IsOverFloor && state.DriftSeconds >= _shipOverboardConfiguration.MinDriftSecondsBeforeCapture)
                return ShipOverboardDecision.EndDrift;

            bool isPastLimit = state.DistanceMeters > _shipOverboardConfiguration.AreaMaxDistanceMeters || state.DriftSeconds > _shipOverboardConfiguration.AreaMaxDriftSeconds;
            return isPastLimit ? ShipOverboardDecision.ReturnToArea : ShipOverboardDecision.None;
        }
    }
}
