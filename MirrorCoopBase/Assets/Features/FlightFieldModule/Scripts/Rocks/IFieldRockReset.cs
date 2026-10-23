namespace Features.FlightFieldModule.Scripts.Rocks {
    // Server: forgets the broken rocks when the field they belong to is published anew or taken down.
    public interface IFieldRockReset {
        public void ServerReset();
    }
}
