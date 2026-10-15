using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // Picks a structure with a chance proportional to its weight.
    public sealed class FieldStructurePicker : IFieldStructurePicker {
        public FieldStructureDefinition PickByWeight(IReadOnlyList<FieldStructureDefinition> structures, IFieldRandom random) {
            float totalWeight = 0f;
            foreach (FieldStructureDefinition structure in structures)
                totalWeight += structure.Weight;

            if (totalWeight <= 0f)
                throw new ArgumentException("No structure with a positive weight to pick from.", nameof(structures));

            float roll = random.Value() * totalWeight;
            foreach (FieldStructureDefinition structure in structures) {
                roll -= structure.Weight;
                if (roll < 0f)
                    return structure;
            }

            return structures[structures.Count - 1];
        }
    }
}
