using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Routes {
    // v1 shaper: reads the corridor parameters from the configuration curves. The corridor is never narrower than
    // the passage clearance, so a corridor without obstacles is always a passage.
    public sealed class CurveFieldRouteShaper : IFieldRouteShaper {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        public CurveFieldRouteShaper(FlightFieldConfiguration flightFieldConfiguration) =>
            _flightFieldConfiguration = flightFieldConfiguration;

        public FieldCorridorShape Shape(float difficulty) {
            FlightFieldConfiguration.RouteShapeCurves curves = _flightFieldConfiguration.RouteShape;
            float clampedDifficulty = Mathf.Clamp01(difficulty);
            int halfWidth = Mathf.Max(_flightFieldConfiguration.PassageClearanceCells, Mathf.RoundToInt(curves.CorridorHalfWidthCells.Evaluate(clampedDifficulty)));
            int maxSideOffset = Mathf.Max(0, Mathf.RoundToInt(curves.MaxSideOffsetCells.Evaluate(clampedDifficulty)));
            float obstacleShare = Mathf.Clamp01(curves.CorridorObstacleShare.Evaluate(clampedDifficulty));
            return new FieldCorridorShape(halfWidth, maxSideOffset, obstacleShare);
        }
    }
}
