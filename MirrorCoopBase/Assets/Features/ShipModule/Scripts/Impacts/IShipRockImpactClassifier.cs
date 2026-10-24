using Features.FlightFieldModule.Contracts;

namespace Features.ShipModule.Scripts.Impacts {
    public interface IShipRockImpactClassifier {
        public ShipRockImpactKind Classify(FieldRockDescription rock);
    }
}
