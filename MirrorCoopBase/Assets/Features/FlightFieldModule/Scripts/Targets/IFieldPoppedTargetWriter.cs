using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Server: writes the synced list of popped target balloons of the current field.
    public interface IFieldPoppedTargetWriter {
        // False while the game scene's live field bridge is not up on this server.
        public bool IsAvailable { get; }
        public void ServerAdd(FieldTargetId id);
        public void ServerClear();
    }
}
