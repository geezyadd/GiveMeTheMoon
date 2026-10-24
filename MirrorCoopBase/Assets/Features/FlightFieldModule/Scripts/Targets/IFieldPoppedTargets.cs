using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Targets {
    // The popped target balloons of the current field, on every peer.
    public interface IFieldPoppedTargets {
        public bool IsPopped(FieldTargetId id);
    }
}
