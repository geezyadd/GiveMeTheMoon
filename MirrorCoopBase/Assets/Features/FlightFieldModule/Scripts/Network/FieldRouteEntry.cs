using System;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Network {
    // A route of the live field without its centerline (the cells travel as FieldRouteCellEntry).
    [Serializable]
    public struct FieldRouteEntry {
        public int Id;
        public FieldRouteKind Kind;
        public float Difficulty;
        public int CorridorWidthCells;
    }
}
