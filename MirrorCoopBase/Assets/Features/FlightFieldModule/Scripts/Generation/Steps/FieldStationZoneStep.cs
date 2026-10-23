using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Reserve: the circles around station A and station B (the approach) stay empty.
    public sealed class FieldStationZoneStep : IFieldGenerationStep {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldAreaReserver _fieldAreaReserver;

        public FieldStationZoneStep(FlightFieldConfiguration flightFieldConfiguration, IFieldAreaReserver fieldAreaReserver) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldAreaReserver = fieldAreaReserver;
        }

        public bool IsUsedBy(FieldLayoutKind layout) =>
            true;

        public void Execute(FieldGenerationContext context) {
            FieldRequest request = context.Request;
            _fieldAreaReserver.Reserve(context, request.StationA, request.StationA, _flightFieldConfiguration.StationAZoneRadius, FieldReserveKind.StationZone);
            _fieldAreaReserver.Reserve(context, request.StationB, request.StationB, _flightFieldConfiguration.StationBZoneRadius, FieldReserveKind.StationZone);
        }
    }
}
