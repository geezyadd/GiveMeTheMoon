namespace Features.ParcelModule.Scripts.Rules {
    public interface IParcelOfferRule {
        // How many parcels a station offers for a roll in 0..1, by the configured weights.
        public int CalculateOfferCount(float roll);
        // A parcel's reward for a roll in 0..1, between the configured bounds, on the reward step.
        public long CalculateReward(float roll);
    }
}
