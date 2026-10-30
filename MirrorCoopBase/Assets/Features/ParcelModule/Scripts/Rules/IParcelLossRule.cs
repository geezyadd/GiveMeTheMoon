using Features.ParcelModule.Data;

namespace Features.ParcelModule.Scripts.Rules {
    public interface IParcelLossRule {
        // heightAboveDeck: the crate's height over the ship's deck plane (negative below it).
        public bool IsLost(ParcelState state, float distanceToShip, float secondsInState, float heightAboveDeck);
    }
}
