using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Where a rock query takes its field from: the server's generated field or the field a peer has from the network.
    public interface IFieldRockFieldSource {
        // LiveFieldNumbers.NO_FIELD while there is no field.
        public int Number { get; }
        // The field's frame as it stands now, after every world shift.
        public FieldFrame Frame { get; }
        public void Read(FieldRockFieldData fieldData);
    }
}
