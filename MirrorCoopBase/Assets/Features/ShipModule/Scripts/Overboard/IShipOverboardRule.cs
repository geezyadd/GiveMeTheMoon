using UnityEngine;

namespace Features.ShipModule.Scripts.Overboard {
    public interface IShipOverboardRule {
        public bool IsInShipVolume(IShipDeckGeometry deckGeometry, Vector3 shipLocal);
        public bool IsOverShipDeck(IShipDeckGeometry deckGeometry, Vector3 shipLocal);
        public bool IsInArea(Bounds floor, Vector3 point);
        public bool IsOverFloor(Bounds floor, Vector3 point);
        public ShipOverboardDecision Decide(in ShipOverboardState state);
    }
}
