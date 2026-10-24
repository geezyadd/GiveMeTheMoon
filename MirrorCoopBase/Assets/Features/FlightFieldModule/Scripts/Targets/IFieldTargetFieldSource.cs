using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Where a target query takes the balloons from: the server's generated field or the field a peer has from the
    // network. A balloon's index is its FieldTargetId on both.
    public interface IFieldTargetFieldSource {
        public bool HasField { get; }
        // The field's frame as it stands now, after every world shift.
        public FieldFrame Frame { get; }
        public int TargetCount { get; }
        public FieldTargetPlacement GetTarget(int index);
    }
}
