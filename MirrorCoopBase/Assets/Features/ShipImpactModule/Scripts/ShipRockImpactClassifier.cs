using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipImpactModule.Scripts {
    // From the rock's data only: the field decides its size (diameter threshold, always-large recipes) and whether it
    // breaks. A rock the ship cannot break knocks it back whatever its size.
    public sealed class ShipRockImpactClassifier : IShipRockImpactClassifier {
        public ShipRockImpactKind Classify(FieldRockDescription rock) =>
            rock.Size == FieldRockSize.Small && rock.IsBreakable ? ShipRockImpactKind.Small : ShipRockImpactKind.Large;
    }
}
