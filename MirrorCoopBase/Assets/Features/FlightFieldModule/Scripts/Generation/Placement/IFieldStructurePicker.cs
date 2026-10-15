using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    public interface IFieldStructurePicker {
        public FieldStructureDefinition PickByWeight(IReadOnlyList<FieldStructureDefinition> structures, IFieldRandom random);
    }
}
