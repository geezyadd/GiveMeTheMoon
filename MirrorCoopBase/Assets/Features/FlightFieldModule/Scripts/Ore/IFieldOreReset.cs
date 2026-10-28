namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: ore state that belongs to one field and is forgotten when a field is published or taken down.
    public interface IFieldOreReset {
        public void ServerReset();
    }
}
