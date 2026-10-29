using Features.ShipModule.Scripts;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    // Where a crate is against the ship's deck.
    public interface IParcelAboardService {
        // Over the deck, from a little below its surface up to the on-board height.
        public bool IsAboard(Vector3 worldPosition, ShipBase ship);
        // Height over the ship's deck plane; negative below it.
        public float GetHeightAboveDeck(Vector3 worldPosition, ShipBase ship);
        // A drifting crate the flying ship has come up to: by the deck's edge and about the deck's height.
        public bool IsFlownInto(Vector3 worldPosition, ShipBase ship);
        // The point over the deck nearest to the crate, from where a crate brought aboard falls onto the deck.
        public Vector3 CalculateDeckDropPoint(Vector3 worldPosition, ShipBase ship);
    }
}
