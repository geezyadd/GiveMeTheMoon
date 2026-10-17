using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Passage;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Check: every main route needs a passage with the ship clearance (alternatives may be closed, dead ends end in a
    // ridge). A blocked main route loses the structures standing within the clearance of its centerline, for a
    // limited number of rounds; the result goes to HasPassage.
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
                if (AreAllRoutesPassable(context)) {
                    context.HasPassage = true;
                    return;
                }

                ClearBlockedRoutes(context);
            }

            context.HasPassage = AreAllRoutesPassable(context);
        }

        private bool AreAllRoutesPassable(FieldGenerationContext context) {
            foreach (FieldRoute route in context.Routes) {
                if (route.Kind == FieldRouteKind.Main && !_fieldPassageChecker.HasPassage(context.Cells, route))
                    return false;
            }

            return true;
        }

        private void ClearBlockedRoutes(FieldGenerationContext context) {
            foreach (FieldRoute route in context.Routes) {
                if (route.Kind == FieldRouteKind.Main && !_fieldPassageChecker.HasPassage(context.Cells, route))
                    _fieldCorridorClearer.Clear(context, route, _flightFieldConfiguration.PassageClearanceCells);
            }
        }
    }
}
