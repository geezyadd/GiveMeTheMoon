using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    public interface IFieldAreaReserver {
        // Reserves every not yet reserved cell within the radius of the segment; a point is a segment of zero length.
        public void Reserve(FieldGenerationContext context, Vector3 segmentStart, Vector3 segmentEnd, float radius, FieldReserveKind reserve);
    }
}
