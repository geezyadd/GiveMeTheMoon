namespace Features.FlightFieldModule.Scripts.Decor {
    public interface IFieldVolumeSourceReader {
        // False while the live field has no main route or its profile is unknown here.
        public bool TryRead(out FieldVolumeSource source);
    }
}
