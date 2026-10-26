using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipImpactModule.Scripts {
    public interface IShipRockImpactClassifier {
        public ShipRockImpactKind Classify(FieldRockDescription rock);
    }
}
