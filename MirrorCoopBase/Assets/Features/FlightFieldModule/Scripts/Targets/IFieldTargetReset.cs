namespace Features.FlightFieldModule.Scripts.Targets {
    // Server: forgets the popped target balloons and their damage when the field they belong to is published anew or
    // taken down.
    public interface IFieldTargetReset {
        public void ServerReset();
    }
}
