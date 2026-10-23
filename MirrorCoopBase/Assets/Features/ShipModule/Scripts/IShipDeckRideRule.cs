using UnityEngine;

namespace Features.ShipModule.Scripts {
    public interface IShipDeckRideRule {
        public ShipRideRefusal CheckBoarding(Vector3 shipLocal);
        public ShipRideRefusal CheckCatch(Vector3 shipLocal, float verticalSpeed, float deckInset);
        public bool TryGetHeightAboveDeck(Vector3 shipLocal, out float height);
    }
}
