using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    public interface IFieldGiantPacker {
        // Closes every unoccupied cell of the class with placements of the options, the largest footprints first.
        public void Pack(FieldGenerationContext context, FieldCellClass cellClass, IReadOnlyList<FieldObstacleOption> options);
    }
}
