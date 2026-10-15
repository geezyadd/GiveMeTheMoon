using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Reserve: the takeoff corridor from station A to the cruise start stays empty; the ship flies it by itself.
    public sealed class FieldTakeoffCorridorStep : IFieldGenerationStep {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldAreaReserver _fieldAreaReserver;

        public FieldTakeoffCorridorStep(FlightFieldConfiguration flightFieldConfiguration, IFieldAreaReserver fieldAreaReserver) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldAreaReserver = fieldAreaReserver;
        }

        public void Execute(FieldGenerationContext context) =>
            _fieldAreaReserver.Reserve(context, context.Request.StationA, context.Request.CruiseStart, _flightFieldConfiguration.TakeoffCorridorHalfWidth,
                FieldReserveKind.TakeoffCorridor);
    }
}
