using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Network;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Server {
    // A hit counts only on a standing container and where the server has it; the damage adds up in the server model.
    public sealed class PointOfInterestDamageService : IPointOfInterestDamageService {
        private readonly IPointsOfInterest _pointsOfInterest;
        private readonly IPointOfInterestZoneRule _pointOfInterestZoneRule;
        private readonly IPointOfInterestContentCatalog _pointOfInterestContentCatalog;
        private readonly IPointOfInterestOpenService _pointOfInterestOpenService;
        private readonly PointOfInterestServerModel _pointOfInterestServerModel;

        public PointOfInterestDamageService(IPointsOfInterest pointsOfInterest, IPointOfInterestZoneRule pointOfInterestZoneRule,
                                            IPointOfInterestContentCatalog pointOfInterestContentCatalog, IPointOfInterestOpenService pointOfInterestOpenService,
                                            PointOfInterestServerModel pointOfInterestServerModel) {
            _pointsOfInterest = pointsOfInterest;
            _pointOfInterestZoneRule = pointOfInterestZoneRule;
            _pointOfInterestContentCatalog = pointOfInterestContentCatalog;
            _pointOfInterestOpenService = pointOfInterestOpenService;
            _pointOfInterestServerModel = pointOfInterestServerModel;
        }

        public bool IsStanding(int pointId) =>
            _pointsOfInterest.TryFindIndex(pointId, out int index) && _pointsOfInterest.Get(index).State != PointOfInterestState.Opened;

        public void ServerApplyHit(int pointId, Vector3 hitPoint, float damage) {
            if (!_pointsOfInterest.TryFindIndex(pointId, out int index))
                return;

            PointOfInterestEntry point = _pointsOfInterest.Get(index);
            PointOfInterestContentDefinition content = _pointOfInterestContentCatalog.GetRequired(point.ContentId);
            bool isHitOnStanding = point.State != PointOfInterestState.Opened && _pointOfInterestZoneRule.IsOnContainer(point.Position, hitPoint, content.ContainerRadiusMeters);
            if (!isHitOnStanding)
                return;

            if (_pointOfInterestServerModel.AddDamage(pointId, damage) >= content.ContainerHitPoints)
                _pointOfInterestOpenService.ServerOpen(pointId);
        }
    }
}
