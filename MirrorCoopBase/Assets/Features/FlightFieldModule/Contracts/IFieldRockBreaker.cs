using Features.NetworkModelModule.Scripts;

namespace Features.FlightFieldModule.Contracts {
    // Server: the one way to break a rock of the current field (ship collisions, weapons). Every peer hides it and
    // IFieldRockEvents.RockDestroyed fires once. The weapon id names the weapon of a Weapon break and stays empty
    // for any other cause.
    public interface IFieldRockBreaker {
        public FieldRockBreakResult ServerBreak(FieldRockId id, FieldRockDestroyCause cause, PlayerKey instigator, string weaponId = "");
    }
}
