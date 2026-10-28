using Features.FlightFieldModule.Scripts.Network;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: the one writer of the synced ore list. Unavailable while no live field bridge is bound.
    public interface IFieldOreWriter {
        public bool IsAvailable { get; }

        public void ServerAdd(FieldOreEntry entry);
        public void ServerSet(int index, FieldOreEntry entry);
        public void ServerRemoveAt(int index);
    }
}
