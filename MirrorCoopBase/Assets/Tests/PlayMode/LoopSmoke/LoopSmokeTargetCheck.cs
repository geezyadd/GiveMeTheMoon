#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Targets;
using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Turret;
using Features.ShopModule.Scripts.Generated;
using Features.WeaponModule.Contracts;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // Target balloons in cruise on the host. The ship is put beside a balloon (the debug move of the debug window's rock aim) and
    // the seated host shoots it with the minigun through the station's test hook; then the ship is put before another
    // balloon and flies into it (the real hull check). Each pops once (in the synced popped list, TargetPopped once,
    // Weapon by the host with the minigun / ShipCollision), is hidden on the host, and the synced crew balance grows by
    // the rewards of the balloons that popped. After the landing the popped list and the balloons are gone.
    public sealed class LoopSmokeTargetCheck {
        private const string MINIGUN_ID = "minigun";
        private const int FIELD_ROCK_LAYER_MASK = 1 << 8;
        private const float SEARCH_RADIUS = 3000f;
        private const float SHOT_DISTANCE = 70f;
        // The turret on the deck looks a little down at a balloon at the ship's height: the ship stands this much lower.
        private const float SHOT_HEIGHT_BELOW = 4f;
        private const float FLY_IN_DISTANCE = 35f;
        private const float POP_TIMEOUT_SECONDS = 6f;
        private const int CANDIDATES_TRIED = 12;

        private readonly LoopSmokeSession _session;
        private readonly LoopSmokeFlight _flight;
        private readonly IFieldTargetQuery _fieldTargetQuery;
        private readonly IFieldTargetEvents _fieldTargetEvents;
        private readonly IFieldTargetWeaponIds _fieldTargetWeaponIds;
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly IReadOnlyFieldTargetSceneModel _fieldTargetSceneModel;
        private readonly IReadOnlyWalletModel _walletModel;
        private readonly IPlayerIdentityService _playerIdentityService;
        private readonly IShipRockImpactDebug _shipRockImpactDebug;
        private readonly List<FieldTargetPopped> _popped = new List<FieldTargetPopped>();
        private readonly List<FieldTargetDescription> _targets = new List<FieldTargetDescription>();

        public LoopSmokeTargetCheck(LoopSmokeContext context) {
            _session = context.Session;
            _flight = context.Flight;
            _fieldTargetQuery = SessionQueries.Resolve<IFieldTargetQuery>();
            _fieldTargetEvents = SessionQueries.Resolve<IFieldTargetEvents>();
            _fieldTargetWeaponIds = SessionQueries.Resolve<IFieldTargetWeaponIds>();
            _liveFieldModel = SessionQueries.Resolve<IReadOnlyLiveFieldModel>();
            _fieldTargetSceneModel = SessionQueries.Resolve<IReadOnlyFieldTargetSceneModel>();
            _walletModel = SessionQueries.Resolve<IReadOnlyWalletModel>();
            _playerIdentityService = SessionQueries.Resolve<IPlayerIdentityService>();
            _shipRockImpactDebug = SessionQueries.Resolve<IShipRockImpactDebug>();
        }

        // The host sits at the turret.
        [LoopSmokeStep(LoopSmokePhase.InCruise, 2, 2, 30)]
        public IEnumerator ShootBalloonCoroutine() {
            ShipBase ship = _session.Ship;
            ShipTurretStation station = LoopSmokeTurretCheck.FindSeatedStation(ship);
            long balanceBefore = _walletModel.Balance;
            _popped.Clear();
            _fieldTargetEvents.TargetPopped += _popped.Add;
            FieldTargetDescription target = default;
            bool isInSight = false;
            foreach (FieldTargetDescription candidate in FindCandidates(ship.transform.position)) {
                _flight.PutShipBeside(candidate.Center, SHOT_DISTANCE, SHOT_HEIGHT_BELOW);
                yield return null;
                yield return null;
                if (!IsBuiltAndFirstStruck(station.transform.position, candidate))
                    continue;

                target = candidate;
                isInSight = true;
                break;
            }

            Assert.IsTrue(isInSight, "Balloon shot: no balloon of the field is in clear sight of the turret.");
            station.DebugHoldTrigger(true);
            yield return _session.WaitForCoroutine(() => AimAndCheckPopped(station, target), POP_TIMEOUT_SECONDS, $"Balloon shot: the minigun popped balloon {target.Id}");
            station.DebugHoldTrigger(false);
            yield return null;
            _fieldTargetEvents.TargetPopped -= _popped.Add;

            FieldTargetPopped popped = AssertPoppedOnce(target, FieldTargetPopCause.Weapon, balanceBefore, "Balloon shot");
            Assert.AreEqual(_playerIdentityService.GetKey(NetworkServer.localConnection), popped.Shooter, "Balloon shot: the balloon was not popped by the host.");
            Assert.AreEqual(MINIGUN_ID, popped.WeaponId);
            Debug.Log($"[LoopSmoke] the minigun popped balloon {target.Id} ({target.KindId}, +{target.Reward}), balance {balanceBefore} -> {_walletModel.Balance}");
        }

        // After the host stood up from the turret. Then the ship goes back to where it was and faces the destination.
        [LoopSmokeStep(LoopSmokePhase.InCruise, 2, 2, 50)]
        public IEnumerator FlyIntoBalloonCoroutine() {
            ShipBase ship = _session.Ship;
            Vector3 start = ship.transform.position;
            long balanceBefore = _walletModel.Balance;
            _popped.Clear();
            _fieldTargetEvents.TargetPopped += _popped.Add;
            List<FieldTargetDescription> candidates = FindCandidates(start);
            Assert.IsNotEmpty(candidates, "Balloon fly-in: no balloon left in the field.");
            FieldTargetDescription target = candidates[0];
            PutShipBefore(ship, target);
            yield return _session.WaitForCoroutine(() => _liveFieldModel.PoppedTargets.Contains(target.Id), POP_TIMEOUT_SECONDS, $"Balloon fly-in: the ship popped balloon {target.Id}");
            yield return null;
            _fieldTargetEvents.TargetPopped -= _popped.Add;

            AssertPoppedOnce(target, FieldTargetPopCause.ShipCollision, balanceBefore, "Balloon fly-in");
            Debug.Log($"[LoopSmoke] the ship flew into balloon {target.Id} ({target.KindId}, +{target.Reward}), balance {balanceBefore} -> {_walletModel.Balance}");
            yield return _flight.PutShipBackCoroutine(start);
        }

        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 2, 2, 10)]
        public void AssertPoppedListClearedAfterLanding() {
            Assert.IsEmpty(_liveFieldModel.PoppedTargets, "The popped balloons survived the landing.");
            Assert.IsEmpty(_liveFieldModel.Targets, "The field's balloons survived the landing.");
        }

        private FieldTargetPopped AssertPoppedOnce(FieldTargetDescription target, FieldTargetPopCause cause, long balanceBefore, string check) {
            List<FieldTargetPopped> ofTarget = _popped.Where(popped => popped.Id.Equals(target.Id)).ToList();
            Assert.AreEqual(1, ofTarget.Count, $"{check}: TargetPopped must fire once for balloon {target.Id}.");
            Assert.AreEqual(cause, ofTarget[0].Cause, $"{check}: balloon {target.Id} popped by another cause.");
            Assert.IsTrue(_liveFieldModel.PoppedTargets.Contains(target.Id), $"{check}: balloon {target.Id} is not in the synced popped list.");
            Assert.AreEqual(_liveFieldModel.PoppedTargets.Count, _liveFieldModel.PoppedTargets.Distinct().Count(), $"{check}: a balloon is twice in the popped list.");
            Assert.IsFalse(_fieldTargetSceneModel.TryGetView(target.Id.Index, out _), $"{check}: popped balloon {target.Id} is still shown on the host.");
            long rewards = _popped.Sum(popped => (long)popped.Reward);
            Assert.AreEqual(balanceBefore + rewards, _walletModel.Balance, $"{check}: the crew balance did not grow by the rewards of the {_popped.Count} popped balloons.");
            return ofTarget[0];
        }

        private List<FieldTargetDescription> FindCandidates(Vector3 from) {
            _fieldTargetQuery.FindInSphere(from, SEARCH_RADIUS, _targets);
            return _targets.OrderBy(target => (target.Center - from).sqrMagnitude).Take(CANDIDATES_TRIED).ToList();
        }

        // Before the balloon at the height of the hull's middle, heading at it.
        private void PutShipBefore(ShipBase ship, FieldTargetDescription target) {
            Vector3 toTarget = LoopSmokeFlight.HorizontalDirection(ship.transform.position, target.Center);
            float hullMiddle = _shipRockImpactDebug.DebugGetHullMiddleHeight();
            Vector3 position = target.Center - toTarget * (target.Radius + FLY_IN_DISTANCE) + Vector3.down * hullMiddle;
            ship.FlightControlDebug.DebugMove(position - ship.transform.position);
            ship.FlightControlDebug.DebugFace(toTarget);
        }

        private bool AimAndCheckPopped(ShipTurretStation station, FieldTargetDescription target) {
            station.DebugAimAt(target.Center);
            return _liveFieldModel.PoppedTargets.Contains(target.Id);
        }

        private bool IsBuiltAndFirstStruck(Vector3 origin, FieldTargetDescription target) {
            if (!_fieldTargetSceneModel.TryGetView(target.Id.Index, out FieldTargetView _))
                return false;

            Vector3 toTarget = target.Center - origin;
            RaycastHit[] hits = Physics.RaycastAll(origin, toTarget.normalized, toTarget.magnitude, FIELD_ROCK_LAYER_MASK, QueryTriggerInteraction.Collide);
            foreach (RaycastHit hit in hits.OrderBy(hit => hit.distance)) {
                bool hasTarget = hit.collider.TryGetComponent(out IWeaponTargetCollider collider);
                if (hit.collider.isTrigger && !hasTarget)
                    continue;

                return hasTarget && collider.TargetId == _fieldTargetWeaponIds.GetTargetId(target.Id);
            }

            return false;
        }
    }
}
#endif
