using Features.NetworkModelModule.Scripts;

namespace Features.FlightFieldModule.Contracts {
    // Server: the one way to break a rock of the current field (ship collisions, weapons). Every peer hides it and
    // IFieldRockEvents.RockDestroyed fires once.
    public interface IFieldRockBreaker {
        public FieldRockBreakResult ServerBreak(FieldRockId id, FieldRockDestroyCause cause, PlayerKey instigator);
    }
}
