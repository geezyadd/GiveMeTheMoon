using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Generation {
    // Runs the generation steps for a request with its seed.
    public sealed class FlightFieldGenerator : IFlightFieldGenerator {
        private readonly List<IFieldGenerationStep> _steps;
        private readonly IFieldRandomFactory _fieldRandomFactory;

        public FlightFieldGenerator(List<IFieldGenerationStep> steps, IFieldRandomFactory fieldRandomFactory) {
            _steps = steps;
            _fieldRandomFactory = fieldRandomFactory;
        }

        public FlightField Generate(FieldRequest request) {
            FieldGenerationContext context = new(request, request.Seed, _fieldRandomFactory.Create(request.Seed));
            foreach (IFieldGenerationStep step in _steps)
                step.Execute(context);

            return CreateField(context);
        }

        private static FlightField CreateField(FieldGenerationContext context) =>
            new(context.Frame, context.Seed, context.Cells, context.Placements, context.Routes, context.StationACell, context.StationBCell,
                context.CruiseStartCell, context.Profile.Id, context.Difficulty, context.Density, context.Metrics, context.Request.LoopIndex, context.Targets);
    }
}
