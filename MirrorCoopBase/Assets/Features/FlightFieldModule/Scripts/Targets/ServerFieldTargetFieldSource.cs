using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Server;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Server: the balloons of the generated field, from the moment it stands, before it is published.
    public sealed class ServerFieldTargetFieldSource : IFieldTargetFieldSource {
        private readonly IReadOnlyFlightFieldServerModel _flightFieldServerModel;

        public bool HasField => _flightFieldServerModel.HasField;
        public FieldFrame Frame => _flightFieldServerModel.Frame;
        public int TargetCount => _flightFieldServerModel.Field.Targets.Count;

        public ServerFieldTargetFieldSource(IReadOnlyFlightFieldServerModel flightFieldServerModel) =>
            _flightFieldServerModel = flightFieldServerModel;

        public FieldTargetPlacement GetTarget(int index) =>
            _flightFieldServerModel.Field.Targets[index];
    }
}
