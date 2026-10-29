using Features.FlightFieldModule.Contracts;
using Features.GameCoreModule.Contracts;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Network;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.PointOfInterestModule.Scripts.Server {
    // Server, every frame: a new field (or none) places its points; then the ship entering a dormant point's zone
    // starts its content, and the hull reaching a standing reward container opens it.
    public sealed class PointOfInterestServerSystem : ITickable {
        private readonly IFieldPointOfInterestSource _fieldPointOfInterestSource;
        private readonly IPointsOfInterest _pointsOfInterest;
        private readonly IPointOfInterestWriter _pointOfInterestWriter;
        private readonly IShipPositionSource _shipPositionSource;
        private readonly IPointOfInterestPlacementService _pointOfInterestPlacementService;
        private readonly IPointOfInterestTriggerService _pointOfInterestTriggerService;
        private readonly IPointOfInterestOpenService _pointOfInterestOpenService;
        private readonly IPointOfInterestZoneRule _pointOfInterestZoneRule;
        private readonly IPointOfInterestContentCatalog _pointOfInterestContentCatalog;
        private readonly PointOfInterestServerModel _pointOfInterestServerModel;

        public PointOfInterestServerSystem(
            IFieldPointOfInterestSource fieldPointOfInterestSource,
            IPointsOfInterest pointsOfInterest,
            IPointOfInterestWriter pointOfInterestWriter,
            IShipPositionSource shipPositionSource,
            IPointOfInterestPlacementService pointOfInterestPlacementService,
            IPointOfInterestTriggerService pointOfInterestTriggerService,
            IPointOfInterestOpenService pointOfInterestOpenService,
            IPointOfInterestZoneRule pointOfInterestZoneRule,
            IPointOfInterestContentCatalog pointOfInterestContentCatalog,
            PointOfInterestServerModel pointOfInterestServerModel) {
            _fieldPointOfInterestSource = fieldPointOfInterestSource;
            _pointsOfInterest = pointsOfInterest;
            _pointOfInterestWriter = pointOfInterestWriter;
            _shipPositionSource = shipPositionSource;
            _pointOfInterestPlacementService = pointOfInterestPlacementService;
            _pointOfInterestTriggerService = pointOfInterestTriggerService;
            _pointOfInterestOpenService = pointOfInterestOpenService;
            _pointOfInterestZoneRule = pointOfInterestZoneRule;
            _pointOfInterestContentCatalog = pointOfInterestContentCatalog;
            _pointOfInterestServerModel = pointOfInterestServerModel;
        }

        public void Tick() {
            if (!NetworkServer.active || !_pointOfInterestWriter.IsAvailable)
                return;

            if (_fieldPointOfInterestSource.FieldNumber != _pointOfInterestServerModel.PlacedFieldNumber)
                _pointOfInterestPlacementService.ServerPlaceForCurrentField();

            if (_shipPositionSource.TryGetShipPosition(out Vector3 shipPosition))
                CheckShip(shipPosition);
        }

        private void CheckShip(Vector3 shipPosition) {
            for (int i = 0; i < _pointsOfInterest.Count; i++) {
                PointOfInterestEntry point = _pointsOfInterest.Get(i);
                if (point.State == PointOfInterestState.Dormant && _pointOfInterestZoneRule.IsInZone(point.Position, shipPosition))
                    _pointOfInterestTriggerService.ServerTrigger(point.Id);

                if (point.State != PointOfInterestState.Opened && IsRamming(point, shipPosition))
                    _pointOfInterestOpenService.ServerOpen(point.Id);
            }
        }

        private bool IsRamming(in PointOfInterestEntry point, Vector3 shipPosition) =>
            _pointOfInterestZoneRule.IsRamming(point.Position, shipPosition, _pointOfInterestContentCatalog.GetRequired(point.ContentId).ContainerRadiusMeters);
    }
}
