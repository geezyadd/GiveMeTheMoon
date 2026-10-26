using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    public interface IFieldRockReader {
        // Every rock of one placement (none for a networked structure) or one debris cell, broken or not, in the
        // world of the given frame, by rock index. The list is cleared and refilled.
        public void ReadPlacement(FieldRockFieldData fieldData, FieldFrame frame, int placementIndex, List<FieldRockDescription> rocks);
        public void ReadDebrisCell(FieldRockFieldData fieldData, FieldFrame frame, Vector2Int cell, List<FieldRockDescription> rocks);
    }
}
