using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Picks a structure with a chance proportional to its weight.
    public sealed class FieldStructurePicker : IFieldStructurePicker {
        public FieldStructureDefinition PickByWeight(IReadOnlyList<FieldStructureDefinition> structures, IFieldRandom random) =>
            FieldWeightedPick.Pick(structures, random);
    }
}
