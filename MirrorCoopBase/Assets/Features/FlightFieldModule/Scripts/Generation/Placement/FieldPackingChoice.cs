using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Generation.Placement {
    // An obstacle option of a zone in one turn: a long footprint is tried both along and across the field.
    public readonly struct FieldPackingChoice : IFieldWeighted {
        public FieldObstacleOption Option { get; }
        public FieldRotation Rotation { get; }
        public float Weight => Option.Weight;

        public FieldPackingChoice(FieldObstacleOption option, FieldRotation rotation) {
            Option = option;
            Rotation = rotation;
        }
    }
}
