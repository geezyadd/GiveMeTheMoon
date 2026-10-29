using Features.PointOfInterestModule.Scripts.Configuration;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Server {
    public sealed class PointOfInterestZoneRule : IPointOfInterestZoneRule {
        private readonly PointOfInterestConfiguration _pointOfInterestConfiguration;

        public PointOfInterestZoneRule(PointOfInterestConfiguration pointOfInterestConfiguration) =>
            _pointOfInterestConfiguration = pointOfInterestConfiguration;

        public bool IsInZone(Vector3 point, Vector3 shipPosition) =>
            Vector3.Distance(point, shipPosition) <= _pointOfInterestConfiguration.ZoneRadiusMeters;

        public bool IsRamming(Vector3 point, Vector3 shipPosition, float containerRadius) =>
            Vector3.Distance(point, shipPosition) <= containerRadius + _pointOfInterestConfiguration.ShipRamReachMeters;

        public bool IsOnContainer(Vector3 point, Vector3 hitPoint, float containerRadius) =>
            Vector3.Distance(point, hitPoint) <= containerRadius + _pointOfInterestConfiguration.HitToleranceMeters;
    }
}
