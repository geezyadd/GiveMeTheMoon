#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.DebugModule.Contracts;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Network;
using Features.PointOfInterestModule.Scripts.Server;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using UnityEngine;
using Zenject;

namespace Features.PointOfInterestModule.Scripts.Debug {
    // Debug only, on the host in flight: "Move the ship to the nearest point of interest" (just outside its zone,
    // facing it, so the cruise flies the ship in) and "Trigger the point of interest now" (the nearest dormant one,
    // as if the ship entered its zone).
    public sealed class PointOfInterestDebugSystem : IInitializable, IDisposable, IPointOfInterestDebug {
        private const string MOVE_LABEL = "Move the ship to the nearest point of interest";
        private const string MOVE_DESCRIPTION = "Puts the ship just outside the zone of the nearest point of interest whose container stands, facing it; the cruise flies it in.";
        private const string TRIGGER_LABEL = "Trigger the point of interest now";
        private const string TRIGGER_DESCRIPTION = "Starts the nearest dormant point of interest as if the ship entered its zone.";
        private const string NO_POINT_REASON = "No point of interest stands in this field.";
        private const string NO_DORMANT_POINT_REASON = "Every point of interest of this field has started.";
        private const float DIRECTION_EPSILON = 0.0001f;

        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IShipDebugActionConditions _shipDebugActionConditions;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IPointsOfInterest _pointsOfInterest;
        private readonly IPointOfInterestTriggerService _pointOfInterestTriggerService;
        private readonly PointOfInterestConfiguration _pointOfInterestConfiguration;
        private readonly List<IDisposable> _registrations = new();

        public PointOfInterestDebugSystem(
            IDebugActionRegistry debugActionRegistry,
            IShipDebugActionConditions shipDebugActionConditions,
            IShipRunBindingModel shipRunBindingModel,
            IPointsOfInterest pointsOfInterest,
            IPointOfInterestTriggerService pointOfInterestTriggerService,
            PointOfInterestConfiguration pointOfInterestConfiguration) {
            _debugActionRegistry = debugActionRegistry;
            _shipDebugActionConditions = shipDebugActionConditions;
            _shipRunBindingModel = shipRunBindingModel;
            _pointsOfInterest = pointsOfInterest;
            _pointOfInterestTriggerService = pointOfInterestTriggerService;
            _pointOfInterestConfiguration = pointOfInterestConfiguration;
        }

        public void Initialize() {
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.POINTS_OF_INTEREST, MOVE_LABEL, MOVE_DESCRIPTION, ReadMoveReason, MoveFromWindow)));
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.POINTS_OF_INTEREST, TRIGGER_LABEL, TRIGGER_DESCRIPTION, ReadTriggerReason, TriggerFromWindow)));
        }

        public void Dispose() {
            foreach (IDisposable registration in _registrations)
                registration.Dispose();

            _registrations.Clear();
        }

        public bool DebugMoveShipToNearest(out int pointId) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (!TryFindNearest(ship.transform.position, IsStanding, out PointOfInterestEntry point)) {
                pointId = 0;
                return false;
            }

            pointId = point.Id;
            Vector3 towardPoint = FlatDirection(point.Position - ship.transform.position, ship.transform.forward);
            Vector3 outside = point.Position - towardPoint * (_pointOfInterestConfiguration.ZoneRadiusMeters + _pointOfInterestConfiguration.DebugApproachMeters);
            ship.FlightControlDebug.DebugMove(outside - ship.transform.position);
            ship.FlightControlDebug.DebugFace(towardPoint);
            return true;
        }

        public PointOfInterestTriggerResult DebugTriggerNearest(out int pointId) {
            pointId = 0;
            if (!TryFindNearest(_shipRunBindingModel.Ship.transform.position, IsDormant, out PointOfInterestEntry point))
                return PointOfInterestTriggerResult.NotFound;

            pointId = point.Id;
            return _pointOfInterestTriggerService.ServerTrigger(point.Id);
        }

        private string ReadMoveReason() =>
            _shipDebugActionConditions.ReadHostInFlightReason() ?? (HasPoint(IsStanding) ? null : NO_POINT_REASON);

        private string ReadTriggerReason() =>
            _shipDebugActionConditions.ReadHostInFlightReason() ?? (HasPoint(IsDormant) ? null : NO_DORMANT_POINT_REASON);

        private void MoveFromWindow() =>
            DebugMoveShipToNearest(out _);

        private void TriggerFromWindow() =>
            DebugTriggerNearest(out _);

        private bool HasPoint(Func<PointOfInterestEntry, bool> isWanted) =>
            TryFindNearest(Vector3.zero, isWanted, out _);

        private bool TryFindNearest(Vector3 from, Func<PointOfInterestEntry, bool> isWanted, out PointOfInterestEntry nearest) {
            nearest = default;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < _pointsOfInterest.Count; i++) {
                PointOfInterestEntry point = _pointsOfInterest.Get(i);
                float distance = Vector3.Distance(from, point.Position);
                if (!isWanted(point) || distance >= nearestDistance)
                    continue;

                nearest = point;
                nearestDistance = distance;
            }

            return nearestDistance < float.MaxValue;
        }

        private static bool IsStanding(PointOfInterestEntry point) =>
            point.State != PointOfInterestState.Opened;

        private static bool IsDormant(PointOfInterestEntry point) =>
            point.State == PointOfInterestState.Dormant;

        private static Vector3 FlatDirection(Vector3 direction, Vector3 fallback) {
            direction.y = 0f;
            return direction.sqrMagnitude < DIRECTION_EPSILON ? fallback : direction.normalized;
        }
    }
}
#endif
