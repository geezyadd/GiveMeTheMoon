using UnityEngine;

namespace Features.ShipModule.Scripts.Overboard {
    public interface IShipOverboardRule {
        public bool IsInShipVolume(IShipDeckGeometry deckGeometry, Vector3 shipLocal);
        public ShipOverboardDecision Decide(in ShipOverboardState state);
        public bool IsRopeHeld(bool hasHolderWeapon, float holderDistanceMeters, float maxRopeMeters);
    }
}
