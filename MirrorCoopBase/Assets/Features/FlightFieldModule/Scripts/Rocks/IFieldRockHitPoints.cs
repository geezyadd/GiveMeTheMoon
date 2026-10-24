using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    public interface IFieldRockHitPoints {
        public float GetHitPoints(in FieldRockDescription rock);
    }
}
