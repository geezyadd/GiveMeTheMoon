using UnityEngine;

namespace Features.ShipModule.Scripts {
    // One item pinned to the deck, in ship space. Synced per item, so a player who joins gets the whole cargo.
    // Not resting yet: it is falling onto the deck from LocalPosition, and the entry is updated where it comes to rest.
    public struct DeckCargoEntry {
        public uint NetId;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public bool IsResting;
    }
}
