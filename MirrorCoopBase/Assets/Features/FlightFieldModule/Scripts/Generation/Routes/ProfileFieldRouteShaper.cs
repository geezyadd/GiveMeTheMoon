using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // Reads the corridor parameters from the field's difficulty profile. The corridor is never narrower than the
    // passage clearance, so a corridor without placements is always a passage.
    public sealed class ProfileFieldRouteShaper : IFieldRouteShaper {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public ProfileFieldRouteShaper(FlightFieldConfiguration flightFieldConfiguration) =>
            _flightFieldConfiguration = flightFieldConfiguration;

        public FieldCorridorShape Shape(FieldDifficultyProfile profile, float difficulty) {
            float clamped = Mathf.Clamp01(difficulty);
            int halfWidth = Mathf.Max(_flightFieldConfiguration.PassageClearanceCells, Mathf.RoundToInt(profile.CorridorHalfWidthCells.Evaluate(clamped)));
            return new FieldCorridorShape(halfWidth, Mathf.Max(0f, profile.WidthVariationCells.Evaluate(clamped)), Mathf.Max(0f, profile.MeanderAmplitudeCells.Evaluate(clamped)),
                Mathf.Max(1f, profile.MeanderPeriodCells.Evaluate(clamped)), Mathf.Clamp01(profile.CorridorDebrisDensity.Evaluate(clamped)),
                Mathf.Max(0f, profile.MeanderWaves.Evaluate(clamped)), 0f);
        }
    }
}
