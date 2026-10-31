using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Generation.Steps {
    // Places the field's points of interest beside the straight course cruise start -> B and keeps a circle round each
    // free of rocks in the flight plane. Its own random from the field seed: the steps after it draw the same numbers
    // as without it, only the reserved cells change what they place.
    public sealed class FieldPointOfInterestStep : IFieldGenerationStep {
        private const int SEED_SALT = 0x504F49;
        private const float HALF = 0.5f;
        private const float DIRECTION_EPSILON = 0.0001f;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldAreaReserver _fieldAreaReserver;
        private readonly IFieldRandomFactory _fieldRandomFactory;

        public FieldPointOfInterestStep(FlightFieldConfiguration flightFieldConfiguration, IFieldAreaReserver fieldAreaReserver, IFieldRandomFactory fieldRandomFactory) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldAreaReserver = fieldAreaReserver;
            _fieldRandomFactory = fieldRandomFactory;
        }

        public void Execute(FieldGenerationContext context) {
            Vector3 start = context.Request.CruiseStart;
            Vector3 course = context.Request.StationB - start;
            course.y = 0f;
            if (course.sqrMagnitude < DIRECTION_EPSILON)
                return;

            IFieldRandom random = _fieldRandomFactory.Create(context.Seed ^ SEED_SALT);
            int wanted = random.Range(_flightFieldConfiguration.PointOfInterestMinCount, _flightFieldConfiguration.PointOfInterestMaxCount + 1);
            int tries = wanted * _flightFieldConfiguration.PointOfInterestTriesPerPoint;
            for (int attempt = 0; attempt < tries && context.PointsOfInterest.Count < wanted; attempt++)
                TryPlace(context, CreateCandidate(context, random, start, course));
        }

        private Vector3 CreateCandidate(FieldGenerationContext context, IFieldRandom random, Vector3 start, Vector3 course) {
            float along = Mathf.Lerp(_flightFieldConfiguration.PointOfInterestMinCourseShare, _flightFieldConfiguration.PointOfInterestMaxCourseShare, random.Value());
            float side = Mathf.Lerp(_flightFieldConfiguration.PointOfInterestMinSideMeters, _flightFieldConfiguration.PointOfInterestMaxSideMeters, random.Value());
            float sideSign = random.Value() < HALF ? -1f : 1f;
            Vector3 right = Vector3.Cross(Vector3.up, course.normalized);
            Vector3 candidate = start + course * along + right * (side * sideSign);
            candidate.y = context.Frame.Height;
            return candidate;
        }

        private void TryPlace(FieldGenerationContext context, Vector3 candidate) {
            float clearRadius = _flightFieldConfiguration.PointOfInterestClearRadiusMeters;
            if (!IsInsideFrame(context.Frame, candidate, clearRadius) || IsNearAnother(context, candidate))
                return;

            _fieldAreaReserver.Reserve(context, candidate, candidate, clearRadius, FieldReserveKind.PointOfInterest);
            context.AddPointOfInterest(context.Frame.WorldToField(candidate));
        }

        private static bool IsInsideFrame(FieldFrame frame, Vector3 point, float clearRadius) {
            Vector2 plane = frame.WorldToPlane(point);
            bool isInsideWidth = plane.x >= clearRadius && plane.x <= frame.WidthCells * frame.CellSize - clearRadius;
            bool isInsideLength = plane.y >= clearRadius && plane.y <= frame.LengthCells * frame.CellSize - clearRadius;
            return isInsideWidth && isInsideLength;
        }

        private bool IsNearAnother(FieldGenerationContext context, Vector3 candidate) {
            Vector3 fieldCandidate = context.Frame.WorldToField(candidate);
            foreach (Vector3 placed in context.PointsOfInterest) {
                if (Vector3.Distance(placed, fieldCandidate) < _flightFieldConfiguration.PointOfInterestMinSpacingMeters)
                    return true;
            }

            return false;
        }
    }
}
