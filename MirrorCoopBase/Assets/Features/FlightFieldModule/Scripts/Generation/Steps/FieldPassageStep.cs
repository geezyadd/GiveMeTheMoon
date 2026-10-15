using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Passage;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Check: every route needs a passage with the ship clearance. A blocked route loses the structures standing within
    // the clearance of its centerline, for a limited number of rounds; the result goes to HasPassage.
    public sealed class FieldPassageStep : IFieldGenerationStep {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldPassageChecker _fieldPassageChecker;
        private readonly IFieldCorridorClearer _fieldCorridorClearer;

        public FieldPassageStep(FlightFieldConfiguration flightFieldConfiguration, IFieldPassageChecker fieldPassageChecker, IFieldCorridorClearer fieldCorridorClearer) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldPassageChecker = fieldPassageChecker;
            _fieldCorridorClearer = fieldCorridorClearer;
        }

        public void Execute(FieldGenerationContext context) {
            for (int round = 0; round < _flightFieldConfiguration.MaxRepairRounds; round++) {
                if (!TryClearBlockedRoutes(context)) {
                    context.HasPassage = true;
                    return;
                }
            }

            context.HasPassage = !HasBlockedRoute(context);
        }

        private bool TryClearBlockedRoutes(FieldGenerationContext context) {
            bool hasBlockedRoute = false;
            foreach (FieldRoute route in context.Routes) {
                if (_fieldPassageChecker.HasPassage(context.Cells, route))
                    continue;

                hasBlockedRoute = true;
                _fieldCorridorClearer.Clear(context, route, _flightFieldConfiguration.PassageClearanceCells);
            }

            return hasBlockedRoute;
        }

        private bool HasBlockedRoute(FieldGenerationContext context) {
            foreach (FieldRoute route in context.Routes) {
                if (!_fieldPassageChecker.HasPassage(context.Cells, route))
                    return true;
            }

            return false;
        }
    }
}
