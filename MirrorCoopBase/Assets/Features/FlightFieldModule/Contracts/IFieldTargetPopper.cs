using Features.NetworkModelModule.Scripts;

namespace Features.FlightFieldModule.Contracts {
    // Server: the one way to pop a target balloon of the current field (weapons, ship collisions). Every peer hides it
    // and IFieldTargetEvents.TargetPopped fires once. The weapon id names the weapon of a Weapon pop and stays empty
    // for any other cause.
    public interface IFieldTargetPopper {
        public FieldTargetPopResult ServerPop(FieldTargetId id, FieldTargetPopCause cause, PlayerKey shooter, string weaponId = "");
    }
}
