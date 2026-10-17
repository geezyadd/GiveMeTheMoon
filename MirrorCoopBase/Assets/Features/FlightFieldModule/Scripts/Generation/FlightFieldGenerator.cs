using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Passage;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation {
    // Runs the generation steps for a request; a seed without a passage is retried with a derived seed, and as the
    // last resort the route corridors are cleared, so a field without a passage is never returned.
    public sealed class FlightFieldGenerator : IFlightFieldGenerator {
        // A prime step: derived seeds of neighbouring request seeds do not collide soon.
        private const int SEED_ATTEMPT_STEP = 7919;

        private readonly List<IFieldGenerationStep> _steps;
        private readonly IFieldRandomFactory _fieldRandomFactory;
        private readonly IFieldCorridorClearer _fieldCorridorClearer;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public FlightFieldGenerator(List<IFieldGenerationStep> steps, IFieldRandomFactory fieldRandomFactory, IFieldCorridorClearer fieldCorridorClearer,
                                    FlightFieldConfiguration flightFieldConfiguration) {
            _steps = steps;
            _fieldRandomFactory = fieldRandomFactory;
            _fieldCorridorClearer = fieldCorridorClearer;
            _flightFieldConfiguration = flightFieldConfiguration;
        }

        public FlightField Generate(FieldRequest request) {
            FieldGenerationContext context = null;
            for (int attempt = 0; attempt < _flightFieldConfiguration.MaxSeedAttempts; attempt++) {
                context = Run(request, DeriveSeed(request.Seed, attempt));
                if (context.HasPassage)
                    return CreateField(context, false);
            }

            ForcePassage(context);
            return CreateField(context, true);
        }

        private FieldGenerationContext Run(FieldRequest request, int seed) {
            FieldGenerationContext context = new(request, seed, _fieldRandomFactory.Create(seed));
            foreach (IFieldGenerationStep step in _steps)
                step.Execute(context);

            return context;
        }

        private void ForcePassage(FieldGenerationContext context) {
            foreach (FieldRoute route in context.Routes) {
                if (route.Kind == FieldRouteKind.Main)
                    _fieldCorridorClearer.Clear(context, route, route.CorridorHalfWidthCells);
            }

            Debug.LogError($"Flight field: no passage after {_flightFieldConfiguration.MaxSeedAttempts} seeds from seed {context.Request.Seed}; the route corridors were cleared by force.");
        }

        private static int DeriveSeed(int seed, int attempt) =>
            unchecked(seed + attempt * SEED_ATTEMPT_STEP);

        private static FlightField CreateField(FieldGenerationContext context, bool isPassageForced) =>
            new(context.Frame, context.Seed, context.Cells, context.Placements, context.Routes, context.StationACell, context.StationBCell,
                context.CruiseStartCell, isPassageForced, context.Profile.Id, context.Difficulty, context.Density, context.Metrics);
    }
}
