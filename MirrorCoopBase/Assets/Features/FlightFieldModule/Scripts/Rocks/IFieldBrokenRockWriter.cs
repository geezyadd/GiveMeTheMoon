using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Server: writes the synced list of broken rocks of the current field.
    public interface IFieldBrokenRockWriter {
        // False while the game scene's live field bridge is not up on this server.
        public bool IsAvailable { get; }
        public void ServerAdd(FieldRockId id);
        public void ServerClear();
    }
}
