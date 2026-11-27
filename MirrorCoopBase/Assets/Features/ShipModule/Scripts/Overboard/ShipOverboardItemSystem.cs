using System.Collections.Generic;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Overboard {
    // Server, a few times a second: loose items drift in open space like players. An item nobody holds, simulated by
    // the server's physics (not deck cargo, not installed, not held), that is outside the ship's volume and every
    // overboard area loses its gravity and its speed fades to a slow drift; in flight it stays where it was and falls
    // behind the ship. Right over a floor again it gets gravity back; too far from all of them it is removed.
    // Clients see it through the item's network body, so nothing here is synced.
    public sealed class ShipOverboardItemSystem : ITickable {
        private const float CHECK_INTERVAL_SECONDS = 0.2f;

        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipOverboardRule _shipOverboardRule;
        private readonly OverboardAreas _overboardAreas;
        private readonly ShipOverboardConfiguration _shipOverboardConfiguration;
        private readonly Dictionary<Rigidbody, DriftingItem> _drifting = new();
        private readonly List<Rigidbody> _gone = new();
        private readonly List<GameObject> _removed = new();
        private float _nextCheckTime;

        public ShipOverboardItemSystem(
            IShipRunBindingModel shipRunBindingModel,
            IShipOverboardRule shipOverboardRule,
            OverboardAreas overboardAreas,
            ShipOverboardConfiguration shipOverboardConfiguration) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipOverboardRule = shipOverboardRule;
            _overboardAreas = overboardAreas;
            _shipOverboardConfiguration = shipOverboardConfiguration;
        }

        public void Tick() {
            if (NetworkServer.active == false) {
                _drifting.Clear();
                return;
            }

            if (Time.time < _nextCheckTime)
                return;

            _nextCheckTime = Time.time + CHECK_INTERVAL_SECONDS;
            ForgetGoneItems();
            ShipBase ship = _shipRunBindingModel.Ship;
            foreach (NetworkIdentity identity in NetworkServer.spawned.Values)
                if (identity != null && identity.TryGetComponent(out Grabbable grabbable) && grabbable.TryGetComponent(out Rigidbody body))
                    Check(ship, grabbable, body);

            for (int i = 0; i < _removed.Count; i++)
                if (_removed[i] != null)
                    NetworkServer.Destroy(_removed[i]);
            _removed.Clear();
        }

        private void Check(ShipBase ship, Grabbable grabbable, Rigidbody body) {
            bool isDrifting = _drifting.TryGetValue(body, out DriftingItem drifting);
            // Held, pinned on the flying deck or installed: whatever moves it now, it is not drifting any more.
            if (grabbable.CanBeGrabbed == false || body.isKinematic) {
                if (isDrifting)
                    EndDrift(body, drifting);
                return;
            }

            Vector3 position = body.worldCenterOfMass;
            ShipOverboardItemState state = new ShipOverboardItemState(isDrifting, IsInSafeSpace(ship, position),
                IsOverFloor(ship, position), isDrifting ? Time.time - drifting.StartTime : 0f, DistanceToSafeSpace(ship, position));
            switch (_shipOverboardRule.DecideItem(state)) {
                case ShipOverboardItemDecision.BeginDrift:
                    BeginDrift(body);
                    break;
                case ShipOverboardItemDecision.EndDrift:
                    EndDrift(body, drifting);
                    break;
                case ShipOverboardItemDecision.Remove:
                    _drifting.Remove(body);
                    _removed.Add(grabbable.gameObject);
                    break;
            }
        }

        private bool IsInSafeSpace(ShipBase ship, Vector3 position) =>
            (ship != null && _shipOverboardRule.IsInShipVolume(ship.DeckGeometry, ship.ToShipLocal(position)))
            || _overboardAreas.Contains(position);

        private bool IsOverFloor(ShipBase ship, Vector3 position) =>
            (ship != null && _shipOverboardRule.IsOverShipDeck(ship.DeckGeometry, ship.ToShipLocal(position)))
            || _overboardAreas.IsOverFloor(position);

        private float DistanceToSafeSpace(ShipBase ship, Vector3 position) {
            float distance = _overboardAreas.TryFindNearest(position, false, out _, out float areaDistance) ? areaDistance : float.PositiveInfinity;
            if (ship != null)
                distance = Mathf.Min(distance, Vector3.Distance(position, ship.transform.position));
            return float.IsPositiveInfinity(distance) ? 0f : distance;
        }

        private void BeginDrift(Rigidbody body) {
            _drifting[body] = new DriftingItem(Time.time, body.useGravity, body.linearDamping, body.angularDamping);
            body.useGravity = false;
            body.linearDamping = _shipOverboardConfiguration.ItemDriftLinearDamping;
            body.angularDamping = _shipOverboardConfiguration.ItemDriftAngularDamping;
            body.WakeUp();
        }

        private void EndDrift(Rigidbody body, DriftingItem drifting) {
            _drifting.Remove(body);
            body.useGravity = drifting.UsedGravity;
            body.linearDamping = drifting.LinearDamping;
            body.angularDamping = drifting.AngularDamping;
            body.WakeUp();
        }

        private void ForgetGoneItems() {
            foreach (Rigidbody body in _drifting.Keys)
                if (body == null)
                    _gone.Add(body);

            for (int i = 0; i < _gone.Count; i++)
                _drifting.Remove(_gone[i]);
            _gone.Clear();
        }

        private readonly struct DriftingItem {
            public DriftingItem(float startTime, bool usedGravity, float linearDamping, float angularDamping) {
                StartTime = startTime;
                UsedGravity = usedGravity;
                LinearDamping = linearDamping;
                AngularDamping = angularDamping;
            }

            public float StartTime { get; }
            public bool UsedGravity { get; }
            public float LinearDamping { get; }
            public float AngularDamping { get; }
        }
    }
}
