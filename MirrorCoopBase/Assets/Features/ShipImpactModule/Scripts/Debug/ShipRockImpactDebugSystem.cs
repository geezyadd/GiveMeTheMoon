#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.DebugModule.Contracts;
using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Hull;
using UnityEngine;
using Zenject;

namespace Features.ShipImpactModule.Scripts.Debug {
    // Debug only, on the host in flight: the debug window's "Aim at the nearest small / large rock" actions put the
    // ship before the nearest rock of that kind, heading at it, so the hit can be tested and recorded. The move is a
    // shift of the flight path, like a world shift, so it is not swept and gives no velocity.
    public sealed class ShipRockImpactDebugSystem : IInitializable, IDisposable, IShipRockImpactDebug {
        private const string LOG_PREFIX = "[ShipRockImpact] ";
        // From the rock's contact sphere (about its visible surface) to the ship: a few seconds of cruise.
        private const float APPROACH_METERS = 40f;
        // A rock counts as in the ship's way when its middle is this share of its radius above or below the ship.
        private const float VERTICAL_REACH_SHARE = 0.3f;
        private const float MIN_HORIZONTAL_SQR = 0.0001f;
        private const string AIM_SMALL_LABEL = "Aim at the nearest small rock";
        private const string AIM_SMALL_DESCRIPTION = "Moves the flying ship 40 m before the nearest small rock in its way, heading at it: the rock breaks and damages the hull.";
        private const string AIM_LARGE_LABEL = "Aim at the nearest large rock";
        private const string AIM_LARGE_DESCRIPTION = "Moves the flying ship 40 m before the nearest large rock in its way, heading at it: the rock knocks the ship back.";

        private static readonly float[] _searchRadii = { 150f, 400f, 900f };

        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IShipRockImpactClassifier _shipRockImpactClassifier;
        private readonly IShipRockContactRule _shipRockContactRule;
        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IShipDebugActionConditions _shipDebugActionConditions;
        private readonly List<FieldRockDescription> _rocks = new();
        private readonly List<IDisposable> _registrations = new();

        public ShipRockImpactDebugSystem(
            IShipRunBindingModel shipRunBindingModel,
            IFieldRockQuery fieldRockQuery,
            IShipRockImpactClassifier shipRockImpactClassifier,
            IShipRockContactRule shipRockContactRule,
            IDebugActionRegistry debugActionRegistry,
            IShipDebugActionConditions shipDebugActionConditions) {
            _shipRunBindingModel = shipRunBindingModel;
            _fieldRockQuery = fieldRockQuery;
            _shipRockImpactClassifier = shipRockImpactClassifier;
            _shipRockContactRule = shipRockContactRule;
            _debugActionRegistry = debugActionRegistry;
            _shipDebugActionConditions = shipDebugActionConditions;
        }

        public void Initialize() {
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.ROCKS, AIM_SMALL_LABEL, AIM_SMALL_DESCRIPTION, _shipDebugActionConditions.ReadHostInFlightReason, AimAtSmallRock)));
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.ROCKS, AIM_LARGE_LABEL, AIM_LARGE_DESCRIPTION, _shipDebugActionConditions.ReadHostInFlightReason, AimAtLargeRock)));
        }

        public void Dispose() {
            foreach (IDisposable registration in _registrations)
                registration.Dispose();

            _registrations.Clear();
        }

        public bool TryDebugAimAtNearestRock(ShipRockImpactKind kind, out FieldRockDescription rock) {
            rock = default;
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || !ship.IsFlying || !TryFindNearest(ship.transform.position, kind, out rock))
                return false;

            Vector3 shipPosition = ship.transform.position;
            Vector3 direction = rock.Center - shipPosition;
            direction.y = 0f;
            direction = direction.sqrMagnitude > MIN_HORIZONTAL_SQR ? direction.normalized : ship.transform.forward;
            float contactRadius = _shipRockContactRule.CalculateContactRadius(kind, rock.Radius);
            Vector3 target = rock.Center - direction * (contactRadius + APPROACH_METERS);
            target.y = shipPosition.y;
            ship.FlightControlDebug.DebugMove(target - shipPosition);
            ship.FlightControlDebug.DebugFace(direction);
            return true;
        }

        public void DebugMoveShipTo(Vector3 position) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship != null && ship.IsFlying)
                ship.FlightControlDebug.DebugMove(position - ship.transform.position);
        }

        public float DebugGetHullMiddleHeight() =>
            ShipHullBox.Enclose(_shipRunBindingModel.Ship.HullVolume.Boxes).Center.y;

        private void AimAtSmallRock() =>
            LogAim(ShipRockImpactKind.Small);

        private void AimAtLargeRock() =>
            LogAim(ShipRockImpactKind.Large);

        private void LogAim(ShipRockImpactKind kind) {
            bool isAimed = TryDebugAimAtNearestRock(kind, out FieldRockDescription rock);
            UnityEngine.Debug.Log(isAimed
                ? $"{LOG_PREFIX}{kind}: heading at {rock.Id} ({rock.SourceId}, {rock.Diameter:0.0} m)"
                : $"{LOG_PREFIX}{kind}: no rock of this kind near the flying ship");
        }

        private bool TryFindNearest(Vector3 shipPosition, ShipRockImpactKind kind, out FieldRockDescription nearest) {
            nearest = default;
            foreach (float radius in _searchRadii) {
                if (TryFindNearestWithin(shipPosition, radius, kind, out nearest))
                    return true;
            }

            return false;
        }

        private bool TryFindNearestWithin(Vector3 shipPosition, float radius, ShipRockImpactKind kind, out FieldRockDescription nearest) {
            nearest = default;
            _fieldRockQuery.FindInSphere(shipPosition, radius, _rocks);
            float best = float.MaxValue;
            foreach (FieldRockDescription rock in _rocks) {
                float distance = (rock.Center - shipPosition).sqrMagnitude;
                if (distance >= best || !IsInTheWay(rock, kind, shipPosition))
                    continue;

                best = distance;
                nearest = rock;
            }

            return best < float.MaxValue;
        }

        private bool IsInTheWay(FieldRockDescription rock, ShipRockImpactKind kind, Vector3 shipPosition) =>
            _shipRockImpactClassifier.Classify(rock) == kind && Mathf.Abs(rock.Center.y - shipPosition.y) <= rock.Radius * VERTICAL_REACH_SHARE;
    }
}
#endif
