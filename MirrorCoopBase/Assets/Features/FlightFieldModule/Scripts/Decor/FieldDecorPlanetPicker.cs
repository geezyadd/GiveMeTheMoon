using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Scene;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // Which planets a flight shows and where, from the field seed (and the main light, the same on every peer), so
    // every peer sees the same sky. Positions are in field space: x across the route, y up from the flight plane,
    // z along the route. The side of the sky (near the horizon, above, below) comes from the seed and the side
    // weights only; the light just turns the planet within that side so its lit side faces the ship. A planet never
    // enters the field, the volume rock bands or the stations' surroundings, and its far edge stays inside the view reach.
    public sealed class FieldDecorPlanetPicker : IFieldDecorPlanetPicker {
        // Keeps the planet sequence apart from the structure and decor rock sequences of the same seed.
        private const int PLANET_SEED_SALT = 1640531527;
        private const float HALF = 0.5f;

        private readonly FlightFieldDecorConfiguration _flightFieldDecorConfiguration;
        // The highest the volume rock bands reach above (and below) the plane, read once: planets keep clear of it.
        private readonly float _volumeTop;
        private readonly IFieldRandom _random;

        private float ViewReach =>
            _flightFieldDecorConfiguration.ViewFarClipPlane - _flightFieldDecorConfiguration.PlanetFarClipMargin;

        public FieldDecorPlanetPicker(FlightFieldDecorConfiguration flightFieldDecorConfiguration, FlightFieldConfiguration flightFieldConfiguration,
                                      IFieldRandomFactory fieldRandomFactory) {
            _flightFieldDecorConfiguration = flightFieldDecorConfiguration;
            _volumeTop = FieldVolumeHeights.FindTop(flightFieldConfiguration.Volume);
            _random = fieldRandomFactory.Create(0);
        }

        public void Fill(int fieldSeed, Vector3 courseStart, float fieldHalfWidth, List<FieldDecorPlanet> planets) {
            planets.Clear();
            _random.Reseed(unchecked(fieldSeed + PLANET_SEED_SALT));
            for (int i = 0; i < _flightFieldDecorConfiguration.MaxPlanetsPerFlight; i++) {
                if (_random.Value() < _flightFieldDecorConfiguration.PlanetChance && TryCreatePlanet(courseStart, fieldHalfWidth, CreateAcrossRoute(), out FieldDecorPlanet planet))
                    planets.Add(planet);
            }
        }

        public void Fill(int fieldSeed, Vector3 courseStart, float fieldHalfWidth, Vector3 lightTravel, List<FieldDecorPlanet> planets) {
            planets.Clear();
            _random.Reseed(unchecked(fieldSeed + PLANET_SEED_SALT));
            for (int i = 0; i < _flightFieldDecorConfiguration.MaxPlanetsPerFlight; i++) {
                if (_random.Value() < _flightFieldDecorConfiguration.PlanetChance && TryCreatePlanet(courseStart, fieldHalfWidth, lightTravel.normalized, out FieldDecorPlanet planet))
                    planets.Add(planet);
            }
        }

        // Every planet takes the same number of random values, shown or not, so the result depends on the seed, the
        // light and the field width only. A planet that has no room in its side even at its smallest size is not shown.
        private bool TryCreatePlanet(Vector3 courseStart, float fieldHalfWidth, Vector3 preferredDirection, out FieldDecorPlanet planet) {
            planet = default;
            FlightFieldDecorConfiguration.PlanetSide side = FieldWeightedPick.Pick(_flightFieldDecorConfiguration.PlanetSides, _random);
            bool isMirrorRollBelow = _random.Value() < HALF;
            float upOrDown = side.IsMirroredBelowPlane && isMirrorRollBelow ? -1f : 1f;
            FlightFieldDecorConfiguration.PlanetDefinition definition = FieldWeightedPick.Pick(_flightFieldDecorConfiguration.Planets, _random);
            float wantedRadius = Lerp(definition.Diameter) / 2f;
            bool hasRoom = TryCreateDirectionTowards(preferredDirection, side, upOrDown, fieldHalfWidth, definition.Diameter.x / 2f, out Vector3 direction, out float maxRadius);
            float surfaceDistance = Lerp(_flightFieldDecorConfiguration.PlanetDistance);
            float alongRouteRoll = Lerp(_flightFieldDecorConfiguration.PlanetAlongRoute);
            Quaternion rotation = FieldClusterLayout.RandomRotation(_random);
            if (!hasRoom)
                return false;

            float radius = Mathf.Min(wantedRadius, maxRadius);
            float viewReach = ViewReach;
            float centerDistance = Mathf.Min(Mathf.Max(surfaceDistance + radius, RequiredCenterDistance(direction, radius, fieldHalfWidth)), viewReach - radius);
            Vector3 offset = direction * centerDistance;
            float alongRoute = Mathf.Min(ClampAlongRoute(alongRouteRoll, offset), MaxAlongRouteWithinReach(offset, viewReach - radius));
            Vector3 position = courseStart + Vector3.forward * alongRoute + offset;
            planet = new FieldDecorPlanet(definition.Prefab, position, rotation, radius * 2f, side);
            return true;
        }

        // With no main light the planet leans to one side of the route.
        private Vector3 CreateAcrossRoute() =>
            _random.Value() < HALF ? Vector3.right : Vector3.left;

        // Within the side's elevation and the light's azimuth spread, the try closest to the preferred direction wins
        // among the ones with room for the planet's smallest size; every try is first turned out of the course ahead.
        private bool TryCreateDirectionTowards(Vector3 preferredDirection, FlightFieldDecorConfiguration.PlanetSide side, float upOrDown, float fieldHalfWidth, float minRadius,
                                               out Vector3 closest, out float closestMaxRadius) {
            float preferredAzimuth = Mathf.Atan2(preferredDirection.x, preferredDirection.z) * Mathf.Rad2Deg;
            float maxAngle = _flightFieldDecorConfiguration.PlanetMaxAngleFromLight;
            closest = Vector3.zero;
            closestMaxRadius = 0f;
            float closestAngle = float.MaxValue;
            for (int i = 0; i < _flightFieldDecorConfiguration.PlanetPlacementTries; i++) {
                float elevation = upOrDown * Lerp(side.ElevationDegrees);
                float azimuth = TurnOutOfCourse(preferredAzimuth + Mathf.Lerp(-maxAngle, maxAngle, _random.Value()), elevation);
                Vector3 direction = CreateDirection(azimuth, elevation);
                float angle = Vector3.Angle(direction, preferredDirection);
                float maxRadius = MaxRadius(direction, fieldHalfWidth);
                if (angle >= closestAngle || maxRadius < minRadius)
                    continue;

                closest = direction;
                closestAngle = angle;
                closestMaxRadius = maxRadius;
            }

            return closestAngle < float.MaxValue;
        }

        // The angle from the course is acos(cos(elevation) * cos(azimuth)), so the azimuth is pushed out to the
        // smallest one that keeps the minimum angle (none is needed when the elevation alone is enough).
        private float TurnOutOfCourse(float azimuthDegrees, float elevationDegrees) {
            float minCos = Mathf.Cos(_flightFieldDecorConfiguration.PlanetMinAngleFromCourse * Mathf.Deg2Rad);
            float elevationCos = Mathf.Cos(elevationDegrees * Mathf.Deg2Rad);
            float minAzimuth = elevationCos <= minCos ? 0f : Mathf.Acos(minCos / elevationCos) * Mathf.Rad2Deg;
            float azimuth = Mathf.DeltaAngle(0f, azimuthDegrees);
            float turned = Mathf.Max(Mathf.Abs(azimuth), minAzimuth);
            return azimuth < 0f ? -turned : turned;
        }

        // The planet's middle stands at least this far along the direction: its surface keeps the nearest planet
        // distance, keeps clear of the route axis (both station pads stand on it) and of the field beside it or of the
        // volume rock bands above / below it.
        private float RequiredCenterDistance(Vector3 direction, float radius, float fieldHalfWidth) {
            float clearance = _flightFieldDecorConfiguration.PlanetFieldClearance;
            float clearOfStations = (_flightFieldDecorConfiguration.PlanetStationClearance + radius) / AxisShare(direction);
            float besideField = (fieldHalfWidth + clearance + radius) / Mathf.Abs(direction.x);
            float beyondLayers = (_volumeTop + clearance + radius) / Mathf.Abs(direction.y);
            return Mathf.Max(Mathf.Max(_flightFieldDecorConfiguration.PlanetDistance.x + radius, clearOfStations), Mathf.Min(besideField, beyondLayers));
        }

        // The largest radius whose required distance plus the radius stays within the view reach: every term of the
        // required distance is (gap + radius) / share, so the bound is (reach * share - gap) / (1 + share).
        private float MaxRadius(Vector3 direction, float fieldHalfWidth) {
            float viewReach = ViewReach;
            float clearance = _flightFieldDecorConfiguration.PlanetFieldClearance;
            float byNearest = (viewReach - _flightFieldDecorConfiguration.PlanetDistance.x) / 2f;
            float byStations = MaxRadiusFor(viewReach, _flightFieldDecorConfiguration.PlanetStationClearance, AxisShare(direction));
            float besideField = MaxRadiusFor(viewReach, fieldHalfWidth + clearance, Mathf.Abs(direction.x));
            float beyondLayers = MaxRadiusFor(viewReach, _volumeTop + clearance, Mathf.Abs(direction.y));
            return Mathf.Min(Mathf.Min(byNearest, byStations), Mathf.Max(besideField, beyondLayers));
        }

        // A planet further along the route looks closer to the course from the start; it moves back until the
        // course start sees it at least the minimum angle off the course.
        private float ClampAlongRoute(float alongRoute, Vector3 offset) {
            float lateral = new Vector2(offset.x, offset.y).magnitude;
            float maxAlongRoute = lateral / Mathf.Tan(_flightFieldDecorConfiguration.PlanetMinAngleFromCourse * Mathf.Deg2Rad) - offset.z;
            return Mathf.Min(alongRoute, Mathf.Max(0f, maxAlongRoute));
        }

        // The farthest the planet's middle may move along the route while it stays within the reach from the course
        // start: the root of |alongRoute * forward + offset| = reach (the offset alone is always within it).
        private static float MaxAlongRouteWithinReach(Vector3 offset, float reach) {
            float room = offset.z * offset.z + reach * reach - offset.sqrMagnitude;
            return Mathf.Max(0f, Mathf.Sqrt(Mathf.Max(0f, room)) - offset.z);
        }

        private static float MaxRadiusFor(float viewReach, float gap, float share) =>
            (viewReach * share - gap) / (1f + share);

        // The share of the direction away from the route axis (the sine of its angle from the course).
        private static float AxisShare(Vector3 direction) =>
            new Vector2(direction.x, direction.y).magnitude;

        // Azimuth 0 is along the route, 90 across it; elevation is up from the flight plane, negative below it.
        private static Vector3 CreateDirection(float azimuthDegrees, float elevationDegrees) {
            float elevation = elevationDegrees * Mathf.Deg2Rad;
            float azimuth = azimuthDegrees * Mathf.Deg2Rad;
            float flat = Mathf.Cos(elevation);
            return new Vector3(flat * Mathf.Sin(azimuth), Mathf.Sin(elevation), flat * Mathf.Cos(azimuth));
        }

        private float Lerp(Vector2 range) =>
            Mathf.Lerp(range.x, range.y, _random.Value());
    }
}
