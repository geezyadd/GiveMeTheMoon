#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts.Impacts;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only, on the host in flight: F3 puts the ship before the nearest small rock, F4 before the nearest large
    // one, heading at it, so the hit can be tested and recorded. The move is a shift of the flight path, like a world
    // shift, so it is not swept and gives no velocity.
    public sealed class ShipRockImpactDebugSystem : ITickable, IShipRockImpactDebug {
        private const string LOG_PREFIX = "[ShipRockImpact] ";
        // From the rock's surface to the ship: about three seconds of cruise.
        private const float APPROACH_METERS = 40f;
        // A rock counts as in the ship's way when its middle is this share of its radius above or below the ship.
        private const float VERTICAL_REACH_SHARE = 0.3f;
        private const float MIN_HORIZONTAL_SQR = 0.0001f;

        private static readonly float[] _searchRadii = { 150f, 400f, 900f };

        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IShipRockImpactClassifier _shipRockImpactClassifier;
        private readonly List<FieldRockDescription> _rocks = new();

        public ShipRockImpactDebugSystem(IShipRunBindingModel shipRunBindingModel, IFieldRockQuery fieldRockQuery, IShipRockImpactClassifier shipRockImpactClassifier) {
            _shipRunBindingModel = shipRunBindingModel;
            _fieldRockQuery = fieldRockQuery;
            _shipRockImpactClassifier = shipRockImpactClassifier;
        }

        public void Tick() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !NetworkServer.active)
                return;

            if (keyboard.f3Key.wasPressedThisFrame)
                LogAim(ShipRockImpactKind.Small);
            else if (keyboard.f4Key.wasPressedThisFrame)
                LogAim(ShipRockImpactKind.Large);
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
            Vector3 target = rock.Center - direction * (rock.Radius + APPROACH_METERS);
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
