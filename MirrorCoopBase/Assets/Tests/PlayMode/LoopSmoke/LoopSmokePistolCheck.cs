#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Readiness;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // The pistol the way a player uses it: bought at the kiosk and picked up by hand at the station after loop 4 of
    // run 2; in that cruise the host aims the camera through the look input and clicks the fire button (the input service's
    // Fire action, as the mouse sends it). One shot hits the debug dummy, a second click within the 2 s cooldown fires
    // nothing, the HUD ring fills up again, and one shot breaks a small field rock as a Weapon break by the host with
    // the pistol. Then the host drops it on the deck.
    public sealed class LoopSmokePistolCheck {
        private const string PISTOL_ID = "pistol";
        private const string PISTOL_SCREENSHOT = "pistol-in-hand-cooldown.png";
        private const int FIELD_ROCK_LAYER_MASK = 1 << 8;
        private const float AIM_TIMEOUT_SECONDS = 3f;
        private const float SHOT_TIMEOUT_SECONDS = 0.8f;
        private const float ROCK_BREAK_TIMEOUT_SECONDS = 2f;
        private const int SHOT_ATTEMPTS = 3;
        private const float BOARD_DROP_HEIGHT = 0.5f;
        private const float BOARD_SETTLE_SECONDS = 0.6f;
        private const float BLOCKED_SHOT_WAIT_SECONDS = 0.6f;
        private const float COOLDOWN_TIMEOUT_SECONDS = 2.5f;
        private const float COMMAND_TIMEOUT_SECONDS = 3f;
        private const float READY = 1f;
        private const float CANDIDATE_SEARCH_RADIUS = 3000f;
        private const int CANDIDATES_TRIED = 12;
        private const float SHOT_DISTANCE = 40f;
        private const float SHOT_HEIGHT_BELOW = 3f;
        private const float SIGHT_WAIT_SECONDS = 0.5f;
        private const float MAX_SHOT_METERS = 140f;

        private readonly LoopSmokeSession _session;
        private readonly LoopSmokeFlight _flight;
        private readonly List<FieldRockDestroyed> _destroyed = new List<FieldRockDestroyed>();
        private readonly List<FieldRockDescription> _rocks = new List<FieldRockDescription>();

        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static GrabController Hand => Player.GetComponent<GrabController>();

        public LoopSmokePistolCheck(LoopSmokeContext context) {
            _session = context.Session;
            _flight = context.Flight;
        }

        // After the host stood by the turret (LoopSmokeTurretTierCheck, order 10).
        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 2, 5, 20)]
        public IEnumerator BuyAndPickUpPistolCoroutine() {
            HandWeaponDefinition pistol = SessionQueries.Resolve<Features.ShopModule.Scripts.Configurations.ShopCatalog>().HandWeapons.First(handWeapon => handWeapon.Id == PISTOL_ID);
            try {
                CameraLookDriver.DebugPitchOverride = true;
                HandWeaponItem bought = null;
                yield return LoopSmokeKiosk.BuyCoroutine<HandWeaponItem>(_flight, LoopSmokeKiosk.FindHandWeaponEntry(PISTOL_ID), pistol.Price, IsLoosePistol, item => bought = item);
                yield return LoopSmokeKiosk.PickUpCoroutine(bought.GetComponent<Grabbable>());
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            HeldWeaponReadinessModel readiness = SessionQueries.Resolve<HeldWeaponReadinessModel>();
            yield return _session.WaitForCoroutine(() => readiness.IsHolding, COMMAND_TIMEOUT_SECONDS, "Pistol: the HUD shows the pistol in hand");
            Assert.AreEqual(READY, readiness.Readiness, "Pistol: a pistol just picked up is not ready to fire.");
            // The host boards by the turret: in cruise it fires the pistol from there and then sits at the turret
            // (LoopSmokeTurretTierCheck); a move in flight would leave the player behind the flying ship.
            ShipDeckBoarding.PutAbove(Player.GetComponent<CharacterMovableBase>(), _session.Ship, LoopSmokeTurretCheck.GunnerSpot, BOARD_DROP_HEIGHT);
            yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
            Assert.IsTrue(SessionQueries.IsOnDeck(_session.Ship, Player.transform.position), "Pistol: the host did not board by the turret.");
        }

        // After the station marker check of this cruise (order 10), before the host sits at the turret (order 30).
        [LoopSmokeStep(LoopSmokePhase.InCruise, 2, 5, 20)]
        public IEnumerator ShootDummyAndRockCoroutine() {
            Assert.IsTrue(Hand.Held != null && Hand.Held.TryGetComponent(out HandWeaponItem _), "Pistol: the host does not hold the pistol in cruise.");
            try {
                CameraLookDriver.DebugPitchOverride = true;
                yield return ShootDummyAndBlockSecondShotCoroutine();
                yield return ShootSmallRockCoroutine();
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            // The later checks aim with the pitch alone and expect the view along the body.
            HandWeaponAiming.TurnViewBack();
            yield return null;

            Player.GetComponent<LocalPlayerInteraction>().ReleaseHeld();
            HeldWeaponReadinessModel readiness = SessionQueries.Resolve<HeldWeaponReadinessModel>();
            yield return _session.WaitForCoroutine(() => Hand.IsHolding == false && readiness.IsHolding == false, COMMAND_TIMEOUT_SECONDS, "Pistol: dropped, the hand and the HUD ring empty");
        }

        // Test cleanup before the later checks of this station: the dropped pistol slides about the deck in flight
        // and could lie in the crosshair of a later check.
        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 2, 5, 5)]
        public void RemoveDroppedPistol() {
            foreach (HandWeaponItem item in UnityEngine.Object.FindObjectsByType<HandWeaponItem>(FindObjectsSortMode.None))
                NetworkServer.Destroy(item.gameObject);
        }

        private IEnumerator ShootDummyAndBlockSecondShotCoroutine() {
            IWeaponDebugTargets targets = SessionQueries.Resolve<IWeaponDebugTargets>();
            HeldWeaponReadinessModel readiness = SessionQueries.Resolve<HeldWeaponReadinessModel>();
            WeaponDebugTarget target = targets.DebugSpawnTarget();
            Assert.IsNotNull(target, "Pistol: the debug target did not spawn in cruise.");

            // The dummy sways and rides with the ship: a shot may miss it, as a player's may; the next one is tried
            // after the cooldown.
            for (int attempt = 1; target.Hits < 1; attempt++) {
                if (attempt > SHOT_ATTEMPTS)
                    Assert.Fail($"Pistol: none of {SHOT_ATTEMPTS} shots hit the debug target (readiness {readiness.Readiness:0.00}, cursor {Cursor.lockState}).");

                yield return AimViewAtCoroutine(() => TargetCenter(target));
                yield return HandWeaponAiming.ClickFireCoroutine();
                Assert.Less(readiness.Readiness, READY, $"Pistol: click {attempt} on a ready pistol did not fire.");
                for (float waited = 0f; waited < SHOT_TIMEOUT_SECONDS && target.Hits < 1; waited += Time.deltaTime) {
                    HandWeaponAiming.KeepAiming(TargetCenter(target));
                    yield return null;
                }

                if (target.Hits < 1)
                    yield return _session.WaitForCoroutine(() => readiness.Readiness >= READY, COOLDOWN_TIMEOUT_SECONDS, "Pistol: ready again after a missed shot");
            }

            Assert.AreEqual(1, target.Hits, "Pistol: one click hit the debug target more than once.");
            Assert.Less(readiness.Readiness, READY, "Pistol: the HUD ring shows it ready right after a shot.");
            yield return LoopSmokeReviveCheck.CaptureScreenshotCoroutine(PISTOL_SCREENSHOT);

            yield return HandWeaponAiming.ClickFireCoroutine();
            for (float waited = 0f; waited < BLOCKED_SHOT_WAIT_SECONDS; waited += Time.deltaTime) {
                HandWeaponAiming.KeepAiming(TargetCenter(target));
                yield return null;
            }

            Assert.AreEqual(1, target.Hits, "Pistol: a second click within the 2 s cooldown fired again.");
            yield return _session.WaitForCoroutine(() => readiness.Readiness >= READY, COOLDOWN_TIMEOUT_SECONDS, "Pistol: ready again after the cooldown");
            targets.DebugDespawnTarget();
            Debug.Log("[LoopSmoke] pistol: one hit on the dummy, the click within the cooldown was blocked");
        }

        private IEnumerator ShootSmallRockCoroutine() {
            Vector3 start = _session.Ship.transform.position;
            IReadOnlyLiveFieldModel liveField = SessionQueries.Resolve<IReadOnlyLiveFieldModel>();
            IFieldRockEvents rockEvents = SessionQueries.Resolve<IFieldRockEvents>();
            FieldRockDescription rock = default;
            bool hasRock = false;
            foreach (FieldRockDescription candidate in FindSmallRockCandidates(start, liveField)) {
                _flight.PutShipBeside(candidate.Center, SHOT_DISTANCE, SHOT_HEIGHT_BELOW);
                yield return new WaitForSeconds(SIGHT_WAIT_SECONDS);
                yield return AimViewAtCoroutine(() => candidate.Center);
                if (IsFirstStruckByShot(candidate) == false)
                    continue;

                rock = candidate;
                hasRock = true;
                break;
            }

            Assert.IsTrue(hasRock, $"Pistol: none of the {CANDIDATES_TRIED} nearest small rocks came into the pistol's sight with the ship beside it.");
            _destroyed.Clear();
            rockEvents.RockDestroyed += _destroyed.Add;
            yield return HandWeaponAiming.ClickFireCoroutine();
            yield return _session.WaitForCoroutine(() => liveField.BrokenRocks.Contains(rock.Id), ROCK_BREAK_TIMEOUT_SECONDS, $"Pistol: one shot broke rock {rock.Id}");
            rockEvents.RockDestroyed -= _destroyed.Add;
            yield return _flight.PutShipBackCoroutine(start);

            List<FieldRockDestroyed> ofRock = _destroyed.Where(destroyed => destroyed.Id.Equals(rock.Id)).ToList();
            Assert.AreEqual(1, ofRock.Count, $"RockDestroyed must fire once for rock {rock.Id}.");
            Assert.AreEqual(FieldRockDestroyCause.Weapon, ofRock[0].Cause);
            Assert.AreEqual(SessionQueries.Resolve<IPlayerIdentityService>().GetKey(NetworkServer.localConnection), ofRock[0].Instigator, "Pistol: the rock was not broken by the host.");
            Assert.AreEqual(PISTOL_ID, ofRock[0].WeaponId);
            Debug.Log($"[LoopSmoke] the pistol broke rock {rock.Id}: {rock.SourceId}, {rock.Diameter:0.0} m, with one shot");
        }

        // Turns the view through the look input (yaw) and the test pitch until its centre ray points at the target.
        private static IEnumerator AimViewAtCoroutine(Func<Vector3> target) {
            float deadline = Time.realtimeSinceStartup + AIM_TIMEOUT_SECONDS;
            while (HandWeaponAiming.KeepAiming(target()) == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Pistol: the view did not settle on {target()}.");

                yield return null;
            }

            yield return null;
        }

        private List<FieldRockDescription> FindSmallRockCandidates(Vector3 from, IReadOnlyLiveFieldModel liveField) {
            SessionQueries.Resolve<IFieldRockQuery>().FindInSphere(from, CANDIDATE_SEARCH_RADIUS, _rocks);
            return _rocks.Where(rock => rock.IsBreakable && rock.Size == FieldRockSize.Small && liveField.BrokenRocks.Contains(rock.Id) == false)
                .OrderBy(rock => (rock.Center - from).sqrMagnitude)
                .Take(CANDIDATES_TRIED)
                .ToList();
        }

        // The crosshair rests on the rock, and the shot from the muzzle toward that point meets the rock first too.
        private static bool IsFirstStruckByShot(FieldRockDescription rock) {
            Transform view = SessionQueries.Resolve<IGameCameraService>().OutputCamera.transform;
            ulong rockTargetId = SessionQueries.Resolve<IFieldRockTargetIds>().GetTargetId(rock.Id);
            if (TryFindFirstTarget(view.position, view.forward, out ulong viewTargetId, out Vector3 aimPoint) == false || viewTargetId != rockTargetId)
                return false;

            Vector3 muzzle = Hand.Held.GetComponent<HandWeaponItem>().Feel.Muzzle.position;
            return TryFindFirstTarget(muzzle, aimPoint - muzzle, out ulong shotTargetId, out _) && shotTargetId == rockTargetId;
        }

        private static bool TryFindFirstTarget(Vector3 origin, Vector3 direction, out ulong targetId, out Vector3 point) {
            targetId = 0;
            point = default;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, MAX_SHOT_METERS, FIELD_ROCK_LAYER_MASK, QueryTriggerInteraction.Collide);
            foreach (RaycastHit hit in hits.OrderBy(hit => hit.distance)) {
                bool hasTarget = hit.collider.TryGetComponent(out IWeaponTargetCollider target);
                if (hit.collider.isTrigger && hasTarget == false)
                    continue;

                if (hasTarget == false)
                    return false;

                targetId = target.TargetId;
                point = hit.point;
                return true;
            }

            return false;
        }

        private static Vector3 TargetCenter(WeaponDebugTarget target) =>
            target.GetComponentInChildren<Collider>().bounds.center;

        private static bool IsLoosePistol(HandWeaponItem item) =>
            item.Definition.Id == PISTOL_ID && item.GetComponent<Grabbable>().CanBeGrabbed;
    }
}
#endif
