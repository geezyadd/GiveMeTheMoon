#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.NetworkModelModule.Scripts;
using Features.ShipImpactModule.Scripts.Debug;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Generated;
using Features.TurretModule.Scripts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Network;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Steps = Tests.Coop.Shared.CoopWeaponsEconomySteps;

namespace Tests.Coop.Host {
    // The weapons with a real second client process shooting (CoopWeaponsEconomyClientScenarios) and the host checking
    // what arrived: at the station the client fires the pistol at the debug dummy (a click within the 2 s cooldown fires
    // nothing; the host sees the pistol in the client's hand, its trigger and a tracer) and each turret tier from the
    // seat (the host sees the trigger, the tracers and the server's accepted hits; the hits come in batched reports,
    // never more than one per network tick, and each tier keeps to its own fire rate); in the cruise the client breaks a
    // small rock and pops a balloon with the heavy gun, sees a balloon the ship flies into popped, and breaks a rock and
    // pops a balloon with the pistol. Every rock breaks and every balloon pops once, by the client's key and weapon, is
    // paid once and is gone on both peers.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopWeaponsTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const string HAND_WEAPON_FEEL_FRAME = "HandWeaponFeelFrame";
        private const string WEAPON_FEEL_EFFECTS = "WeaponFeelEffects";
        private const string TRACER_PREFIX = "Tracer";
        private const string HIDDEN = "hidden";
        // The pistol model sits at the holder's side (HandWeaponDefinition.HandOffset, about 0.6 m from the arm point).
        private const float PISTOL_AT_HAND_METERS = 1.2f;
        private const float FIRE_WINDOW_SECONDS = 2f;
        // The server's fire rate budget lets a little burst through (WeaponHitRuleConfiguration).
        private const float FIRE_RATE_TOLERANCE = 1.25f;
        private const int FIRE_RATE_BURST = 3;
        private const int TIER_HITS_BEFORE_WINDOW = 1;
        private const float ROCK_SHOT_DISTANCE = 45f;
        private const float BALLOON_SHOT_DISTANCE = 70f;
        private const float PISTOL_SHOT_DISTANCE = 40f;
        // The turret on the deck looks a little up at the target: the ship stands this much lower.
        private const float SHOT_HEIGHT_BELOW = 4f;
        private const float FLY_IN_DISTANCE = 35f;
        private const float SIGHT_WAIT_SECONDS = 0.5f;
        private const float FULL_THROTTLE = 1f;
        private const float STOPPED_SPEED = 0.2f;
        private const float SPEED_SAMPLE_SECONDS = 0.25f;

        private static readonly string[] _turretTiers = { Steps.LIGHT_GUN_ID, Steps.MINIGUN_ID, Steps.HEAVY_GUN_ID };

        private readonly WaitForFixedUpdate _physicsStep = new WaitForFixedUpdate();
        private readonly WaitForSeconds _speedSample = new WaitForSeconds(SPEED_SAMPLE_SECONDS);
        private readonly Dictionary<string, int> _tierWindowHits = new Dictionary<string, int>();
        private readonly List<FieldRockDestroyed> _destroyed = new List<FieldRockDestroyed>();
        private readonly List<FieldTargetPopped> _popped = new List<FieldTargetPopped>();
        private CoopEconomyHostHarness _host;
        private CoopFieldTargets _targets;

        private ShipBase Ship => _host.Ship;
        private static IReadOnlyLiveFieldModel Field => SessionQueries.Resolve<IReadOnlyLiveFieldModel>();
        private static IReadOnlyWalletModel Wallet => SessionQueries.Resolve<IReadOnlyWalletModel>();

        [SetUp]
        public void SetUp() =>
            _host = new CoopEconomyHostHarness();

        [TearDown]
        public void TearDown() =>
            _host.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientFiresPistolAndTurretTiers_AndShootsRocksAndBalloons_ThenHostSeesEveryShotAndPaysOnce() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndStartWithClientCoroutine(Steps.SCENARIO_WEAPONS);
            _host.Flight.InstallRequiredEngines();
            Assert.IsTrue(FindTurretSocket().ServerTryInstall(Steps.LIGHT_GUN_ID), "The light gun was not installed at the station.");
            yield return WatchClientPistolAtDummyCoroutine();
            foreach (string tier in _turretTiers)
                yield return WatchClientTurretTierCoroutine(tier);

            AssertTierFireRates();
            yield return FlyToCruiseWithClientSeatedCoroutine();
            yield return WatchClientTurretShootFieldCoroutine();
            yield return WatchShipFlyIntoBalloonCoroutine();
            yield return WatchClientPistolShootFieldCoroutine();
            yield return LandCoroutine();
            yield return _host.FinishCoroutine();
        }

        // The host puts a pistol into the client's hand (test setup: CoopEconomyTest buys one).
        private IEnumerator WatchClientPistolAtDummyCoroutine() {
            _host.BeginStep("client fires the pistol at the dummy");
            NetworkIdentity client = CoopEconomyHostHarness.ClientPlayer();
            HandWeaponDefinition pistol = SessionQueries.Resolve<ShopCatalog>().HandWeapons[0];
            GameObject item = Object.Instantiate(pistol.ItemPrefab.gameObject, client.transform.position + Vector3.up, client.transform.rotation);
            NetworkServer.Spawn(item);
            yield return null;
            Assert.IsTrue(client.GetComponent<GrabController>().ServerGive(item.GetComponent<Grabbable>()), "The pistol could not be put into the client's hand.");
            IWeaponDebugTargets dummies = SessionQueries.Resolve<IWeaponDebugTargets>();
            WeaponDebugTarget dummy = dummies.DebugSpawnTarget();
            Assert.IsNotNull(dummy, "The debug dummy did not spawn at the station.");

            _host.SendCommand(Steps.COMMAND_FIRE_PISTOL_AT_DUMMY);
            WeaponHolderSync clientWeapon = client.GetComponent<WeaponHolderSync>();
            bool hasSeenTrigger = false;
            bool hasSeenTracer = false;
            float pistolDistance = float.MaxValue;
            yield return _host.WaitForCoroutine(() => {
                hasSeenTrigger |= clientWeapon.IsTriggerHeld;
                hasSeenTracer |= HasActiveTracer(item.scene, HAND_WEAPON_FEEL_FRAME);
                pistolDistance = Mathf.Min(pistolDistance, PistolDistanceFromHolder(item, client));
                return _host.TryTakeReport(Steps.PISTOL_FIRED);
            }, "the client's 'pistol-fired' report");

            Assert.IsTrue(hasSeenTrigger, "The host never saw the client's pistol trigger.");
            Assert.IsTrue(hasSeenTracer, "The host did not replay the client's pistol shot (no tracer).");
            Assert.Less(pistolDistance, PISTOL_AT_HAND_METERS, "The host does not show the pistol in the client's hand.");
            Assert.That(dummy.Hits, Is.InRange(1, 2), "The server must accept one hit per pistol shot: the client fired twice and clicked once within the cooldown.");
            Debug.Log($"[CoopTest] the client's pistol: {dummy.Hits} accepted hits, a click within the cooldown blocked, the pistol {pistolDistance:0.00} m from the client's arm on the host");
            dummies.DebugDespawnTarget();
            _host.AssertSameView(false, true);

            yield return _host.CommandCoroutine(Steps.PISTOL_DROPPED, Steps.COMMAND_DROP_PISTOL);
            Assert.IsTrue(item.GetComponent<Grabbable>().CanBeGrabbed, "The pistol the client dropped cannot be picked up on the host.");
        }

        // The client sits at the turret and holds the trigger on the dummy; the host counts the accepted hits and the
        // weapon's commands over a fixed window after the first hit, then swaps the next tier in.
        private IEnumerator WatchClientTurretTierCoroutine(string tier) {
            _host.BeginStep($"client fires the turret tier {tier}");
            ShipSocket socket = FindTurretSocket();
            if (socket.InstalledModuleId != tier)
                SwapTurretGun(socket, tier);

            yield return _host.CommandCoroutine(Steps.SEATED_AT_TURRET, Steps.COMMAND_SIT_AT_TURRET, tier);
            Assert.AreEqual(tier, _host.LastReport.Message, "The client sits at another gun.");
            Assert.IsTrue(socket.IsHeldBy(CoopEconomyHostHarness.ClientRider().Key), "The turret seat is not held by the client's key on the server.");
            IWeaponDebugTargets dummies = SessionQueries.Resolve<IWeaponDebugTargets>();
            WeaponDebugTarget dummy = dummies.DebugSpawnTarget();
            NetworkIdentity client = CoopEconomyHostHarness.ClientPlayer();
            WeaponHolderSync clientWeapon = client.GetComponent<WeaponHolderSync>();
            bool hasSeenTrigger = false;
            bool hasSeenTracer = false;
            using (CoopWeaponCommandCounter commands = new CoopWeaponCommandCounter(client.netId)) {
                yield return _host.CommandCoroutine(Steps.TURRET_FIRING, Steps.COMMAND_FIRE_TURRET_AT_DUMMY);
                yield return _host.WaitForCoroutine(() => {
                    hasSeenTrigger |= clientWeapon.IsTriggerHeld;
                    hasSeenTracer |= HasActiveTurretTracer(Ship.transform);
                    return dummy.Hits >= TIER_HITS_BEFORE_WINDOW;
                }, $"a hit of the client's {tier} accepted on the dummy");

                int hitsBefore = dummy.Hits;
                int reportsBefore = commands.HitReports;
                int aimsBefore = commands.AimUpdates;
                float windowEnd = Time.realtimeSinceStartup + FIRE_WINDOW_SECONDS;
                yield return _host.WaitForCoroutine(() => {
                    hasSeenTracer |= HasActiveTurretTracer(Ship.transform);
                    return Time.realtimeSinceStartup >= windowEnd;
                }, $"the {tier} fire window");

                int hits = dummy.Hits - hitsBefore;
                int reports = commands.HitReports - reportsBefore;
                int aims = commands.AimUpdates - aimsBefore;
                yield return _host.CommandCoroutine(Steps.TURRET_FIRED, Steps.COMMAND_STOP_FIRE);
                AssertTierWindow(tier, hits, reports, aims);
            }

            Assert.IsTrue(hasSeenTrigger, $"The host never saw the client's {tier} trigger.");
            Assert.IsTrue(hasSeenTracer, $"The host did not replay the client's {tier} fire (no tracer).");
            dummies.DebugDespawnTarget();
            yield return _host.CommandCoroutine(Steps.STOOD_UP, Steps.COMMAND_STAND_UP);
            Assert.IsFalse(socket.HasOccupant, "The turret seat is still taken after the client stood up.");
            _host.AssertSameView(false, true);
        }

        // The hits keep to the tier's fire rate, and come in batches: never more hit reports than network ticks.
        private void AssertTierWindow(string tier, int hits, int reports, int aims) {
            WeaponDefinition weapon = SessionQueries.Resolve<WeaponCatalog>().Weapons.First(definition => definition.Id == tier);
            int maxHits = Mathf.CeilToInt(weapon.ShotsPerSecond * FIRE_WINDOW_SECONDS * FIRE_RATE_TOLERANCE) + FIRE_RATE_BURST;
            int maxReports = Mathf.CeilToInt(FIRE_WINDOW_SECONDS * NetworkServer.sendRate) + FIRE_RATE_BURST;
            Debug.Log($"[CoopTest] {tier} in {FIRE_WINDOW_SECONDS} s: {hits} hits accepted, {reports} hit reports, {aims} aim updates (rate {weapon.ShotsPerSecond}/s, send rate {NetworkServer.sendRate}/s)");
            Assert.Greater(hits, 0, $"No hit of the client's {tier} was accepted in the fire window.");
            Assert.LessOrEqual(hits, maxHits, $"The server accepted more hits of {tier} than its fire rate allows.");
            Assert.LessOrEqual(reports, maxReports, $"The client's {tier} sent more hit reports than network ticks.");
            Assert.LessOrEqual(reports, hits + FIRE_RATE_BURST, $"The client's {tier} sent more hit reports than hits: no batching.");
            _tierWindowHits[tier] = hits;
        }

        // Tier 2 fires the fastest: in the same window it lands the most hits.
        private void AssertTierFireRates() {
            Assert.Greater(_tierWindowHits[Steps.MINIGUN_ID], _tierWindowHits[Steps.LIGHT_GUN_ID], "The minigun did not land more hits than the light gun in the same window.");
            Assert.Greater(_tierWindowHits[Steps.MINIGUN_ID], _tierWindowHits[Steps.HEAVY_GUN_ID], "The minigun did not land more hits than the heavy gun in the same window.");
        }

        // As a player takes a gun off by the socket's use command (test setup: the client installs one in
        // CoopEconomyTest): the host's hand takes it and the item goes, the next tier goes in.
        private void SwapTurretGun(ShipSocket socket, string tier) {
            NetworkIdentity host = SessionQueries.LocalPlayer;
            GrabController hand = host.GetComponent<GrabController>();
            socket.GetComponent<ShipUninstallInteractable>().ServerUse(host, hand);
            Assert.IsFalse(socket.IsOccupied, $"The host could not take {socket.InstalledModuleId} off the turret socket.");
            hand.ServerConsumeHeld();
            Assert.IsTrue(socket.ServerTryInstall(tier), $"{tier} was not installed in the turret socket.");
        }

        private IEnumerator FlyToCruiseWithClientSeatedCoroutine() {
            _host.BeginStep("loop 1 takeoff with the client at the heavy gun");
            yield return _host.CommandCoroutine(Steps.SEATED_AT_TURRET, Steps.COMMAND_SIT_AT_TURRET, Steps.HEAVY_GUN_ID);
            _host.SendCommand(Steps.COMMAND_RIDE);
            _host.Flight.PullLaunchLever(1);
            yield return _host.WaitForCoroutine(() => _host.Session.Model.Phase == ShipRunPhase.Cruise, "the cruise of loop 1");
            // The short test cruise would land while the client shoots: the ship stops in the cruise until it is done.
            SessionQueries.Resolve<IShipThrottleDebug>().DebugSetThrottle(0f);
            yield return _host.WaitForReportCoroutine(Steps.IN_CRUISE);
            yield return WaitForShipStoppedCoroutine();
            _host.AssertSameView(true, true);
            _targets = new CoopFieldTargets();
        }

        private IEnumerator WatchClientTurretShootFieldCoroutine() {
            _host.BeginStep("client breaks a small rock with the heavy gun");
            Transform turret = FindTurretSocket().GetComponentInChildren<ShipTurretStation>().transform;
            Vector3 start = Ship.transform.position;
            FieldRockDescription rock = default;
            bool hasRock = false;
            foreach (FieldRockDescription candidate in _targets.FindSmallRocks(start)) {
                _host.Flight.PutShipBeside(candidate.Center, ROCK_SHOT_DISTANCE, SHOT_HEIGHT_BELOW);
                yield return WaitForSightCoroutine(() => _targets.IsRockInSight(turret, candidate));
                if (_targets.IsRockInSight(turret, candidate) == false)
                    continue;

                rock = candidate;
                hasRock = true;
                break;
            }

            Assert.IsTrue(hasRock, "None of the nearest small rocks came into the turret's sight.");
            yield return WatchRockBrokenCoroutine(rock, Steps.COMMAND_TURRET_ROCK, Steps.HEAVY_GUN_ID);
            Assert.AreEqual(Steps.ROCK_BROKEN, _host.LastReport.Step, "The client did not shoot the rock in the turret's sight.");
            yield return PutShipBackCoroutine(start);

            _host.BeginStep("client pops a balloon with the heavy gun");
            FieldTargetDescription balloon = default;
            bool hasBalloon = false;
            foreach (FieldTargetDescription candidate in _targets.FindBalloons(start)) {
                _host.Flight.PutShipBeside(candidate.Center, BALLOON_SHOT_DISTANCE, SHOT_HEIGHT_BELOW);
                yield return WaitForSightCoroutine(() => _targets.IsBalloonInSight(turret, candidate));
                if (_targets.IsBalloonInSight(turret, candidate) == false)
                    continue;

                balloon = candidate;
                hasBalloon = true;
                break;
            }

            Assert.IsTrue(hasBalloon, "No balloon of the field came into the turret's sight.");
            yield return WatchBalloonPoppedCoroutine(balloon, Steps.COMMAND_TURRET_BALLOON, Steps.HEAVY_GUN_ID);
            yield return PutShipBackCoroutine(start);
            yield return _host.CommandCoroutine(Steps.STOOD_UP, Steps.COMMAND_STAND_UP);
        }

        // The real hull check pops it: the ship flies at Full before the balloon and stops again.
        private IEnumerator WatchShipFlyIntoBalloonCoroutine() {
            _host.BeginStep("the ship flies into a balloon");
            Vector3 start = Ship.transform.position;
            FieldTargetDescription balloon = _targets.FindBalloons(start).First();
            long balanceBefore = Wallet.Balance;
            _popped.Clear();
            IFieldTargetEvents events = SessionQueries.Resolve<IFieldTargetEvents>();
            events.TargetPopped += _popped.Add;
            IShipThrottleDebug throttle = SessionQueries.Resolve<IShipThrottleDebug>();
            throttle.DebugSetThrottle(FULL_THROTTLE);
            PutShipBefore(balloon);
            yield return _host.WaitForCoroutine(() => Field.PoppedTargets.Contains(balloon.Id), $"the ship to pop balloon {balloon.Id}");
            throttle.DebugSetThrottle(0f);
            yield return null;
            events.TargetPopped -= _popped.Add;
            AssertPoppedOnce(balloon, FieldTargetPopCause.ShipCollision, balanceBefore);
            yield return _host.CommandCoroutine(Steps.BALLOON_POPPED, Steps.COMMAND_SEE_BALLOON, balloon.Id.Index);
            Assert.AreEqual(HIDDEN, _host.LastReport.Message, $"Balloon {balloon.Id} the ship popped is still shown on the client.");
            _host.AssertSameView(true, true);
            yield return PutShipBackCoroutine(start);
            yield return WaitForShipStoppedCoroutine();
        }

        // The client checks the sight from its own view: a target behind something is skipped for the next one.
        private IEnumerator WatchClientPistolShootFieldCoroutine() {
            _host.BeginStep("client picks the pistol up from the deck");
            yield return _host.CommandCoroutine(Steps.PISTOL_PICKED_UP, Steps.COMMAND_PICK_UP_PISTOL);
            Grabbable held = CoopEconomyHostHarness.ClientPlayer().GetComponent<GrabController>().Held;
            Assert.IsTrue(held != null && held.TryGetComponent(out HandWeaponItem _), "The host does not see the pistol in the client's hand.");

            _host.BeginStep("client breaks a small rock with the pistol");
            Vector3 start = Ship.transform.position;
            bool isBroken = false;
            foreach (FieldRockDescription candidate in _targets.FindSmallRocks(start)) {
                _host.Flight.PutShipBeside(candidate.Center, PISTOL_SHOT_DISTANCE, SHOT_HEIGHT_BELOW);
                yield return WaitForSightCoroutine(() => _targets.IsRockShown(candidate.Id));
                yield return WatchRockBrokenCoroutine(candidate, Steps.COMMAND_PISTOL_ROCK, Steps.PISTOL_ID);
                isBroken = _host.LastReport.Step == Steps.ROCK_BROKEN;
                if (isBroken)
                    break;
            }

            Assert.IsTrue(isBroken, "None of the nearest small rocks came into the client's pistol sight.");
            yield return PutShipBackCoroutine(start);

            _host.BeginStep("client pops a balloon with the pistol");
            bool isPopped = false;
            foreach (FieldTargetDescription candidate in _targets.FindBalloons(start)) {
                _host.Flight.PutShipBeside(candidate.Center, PISTOL_SHOT_DISTANCE, SHOT_HEIGHT_BELOW);
                yield return WaitForSightCoroutine(() => _targets.IsBalloonShown(candidate.Id));
                yield return WatchBalloonPoppedCoroutine(candidate, Steps.COMMAND_PISTOL_BALLOON, Steps.PISTOL_ID);
                isPopped = _host.LastReport.Step == Steps.BALLOON_POPPED;
                if (isPopped)
                    break;
            }

            Assert.IsTrue(isPopped, "No balloon came into the client's pistol sight.");
            yield return PutShipBackCoroutine(start);
        }

        // Ends with the client's report; a target the client could not see breaks nothing and is not checked.
        private IEnumerator WatchRockBrokenCoroutine(FieldRockDescription rock, string command, string weaponId) {
            _destroyed.Clear();
            IFieldRockEvents events = SessionQueries.Resolve<IFieldRockEvents>();
            events.RockDestroyed += _destroyed.Add;
            _host.SendCommand(command, FieldRockIdText.Format(rock.Id));
            yield return _host.WaitForReportCoroutine(Steps.ROCK_BROKEN, Steps.TARGET_BLOCKED);
            yield return null;
            events.RockDestroyed -= _destroyed.Add;
            if (_host.LastReport.Step == Steps.TARGET_BLOCKED) {
                Debug.Log($"[CoopTest] the client skipped {command}: {_host.LastReport.Message}");
                yield break;
            }

            List<FieldRockDestroyed> ofRock = _destroyed.Where(destroyed => destroyed.Id.Equals(rock.Id)).ToList();
            Assert.AreEqual(1, ofRock.Count, $"RockDestroyed must fire once for rock {rock.Id}.");
            Assert.AreEqual(FieldRockDestroyCause.Weapon, ofRock[0].Cause, $"Rock {rock.Id} broke by another cause.");
            Assert.AreEqual(ClientKey(), ofRock[0].Instigator, $"Rock {rock.Id} was not broken by the client.");
            Assert.AreEqual(weaponId, ofRock[0].WeaponId, $"Rock {rock.Id} was not broken by the client's {weaponId}.");
            Assert.IsFalse(_targets.IsRockShown(rock.Id), $"Rock {rock.Id} the client broke is still shown on the host.");
            Assert.AreEqual(HIDDEN, _host.LastReport.Message, $"Rock {rock.Id} the client broke is still shown on the client.");
            Debug.Log($"[CoopTest] the client's {weaponId} broke rock {rock.Id} ({rock.SourceId}, {rock.Diameter:0.0} m)");
            _host.AssertSameView(true, true);
        }

        private IEnumerator WatchBalloonPoppedCoroutine(FieldTargetDescription balloon, string command, string weaponId) {
            long balanceBefore = Wallet.Balance;
            _popped.Clear();
            IFieldTargetEvents events = SessionQueries.Resolve<IFieldTargetEvents>();
            events.TargetPopped += _popped.Add;
            _host.SendCommand(command, balloon.Id.Index);
            CoopWeaponCommandCounter commands = new CoopWeaponCommandCounter(CoopEconomyHostHarness.ClientPlayer().netId);
            try {
                yield return _host.WaitForReportCoroutine(Steps.BALLOON_POPPED, Steps.TARGET_BLOCKED);
            } finally {
                commands.Dispose();
                Debug.Log($"[CoopTest] the client's {weaponId} on balloon {balloon.Id} ({balloon.KindId}, {balloon.Radius} m at {balloon.Center}): {commands.HitReports} hit reports, {commands.AimUpdates} trigger / aim updates");
            }

            yield return null;
            events.TargetPopped -= _popped.Add;
            if (_host.LastReport.Step == Steps.TARGET_BLOCKED) {
                Debug.Log($"[CoopTest] the client skipped {command}: {_host.LastReport.Message}");
                yield break;
            }

            FieldTargetPopped popped = AssertPoppedOnce(balloon, FieldTargetPopCause.Weapon, balanceBefore);
            Assert.AreEqual(ClientKey(), popped.Shooter, $"Balloon {balloon.Id} was not popped by the client.");
            Assert.AreEqual(weaponId, popped.WeaponId, $"Balloon {balloon.Id} was not popped by the client's {weaponId}.");
            Assert.AreEqual(HIDDEN, _host.LastReport.Message, $"Balloon {balloon.Id} the client popped is still shown on the client.");
            _host.AssertSameView(true, true);
        }

        private FieldTargetPopped AssertPoppedOnce(FieldTargetDescription balloon, FieldTargetPopCause cause, long balanceBefore) {
            List<FieldTargetPopped> ofBalloon = _popped.Where(popped => popped.Id.Equals(balloon.Id)).ToList();
            Assert.AreEqual(1, ofBalloon.Count, $"TargetPopped must fire once for balloon {balloon.Id}.");
            Assert.AreEqual(cause, ofBalloon[0].Cause, $"Balloon {balloon.Id} popped by another cause.");
            Assert.AreEqual(Field.PoppedTargets.Count, Field.PoppedTargets.Distinct().Count(), "A balloon is twice in the popped list.");
            Assert.IsFalse(_targets.IsBalloonShown(balloon.Id), $"Popped balloon {balloon.Id} is still shown on the host.");
            long rewards = _popped.Sum(popped => (long)popped.Reward);
            Assert.AreEqual(balanceBefore + rewards, Wallet.Balance, $"The crew balance did not grow once by the rewards of the {_popped.Count} popped balloons.");
            Debug.Log($"[CoopTest] balloon {balloon.Id} ({balloon.KindId}, +{balloon.Reward}) popped by {cause}, balance {balanceBefore} -> {Wallet.Balance}");
            return ofBalloon[0];
        }

        private IEnumerator LandCoroutine() {
            _host.BeginStep("loop 1 landing");
            _host.SendCommand(Steps.COMMAND_LAND);
            // The flight applies the last debug move in its next step: the pilot faces the destination from there.
            yield return null;
            _host.Flight.FaceDestination();
            SessionQueries.Resolve<IShipThrottleDebug>().DebugSetThrottle(FULL_THROTTLE);
            yield return _host.WaitForCoroutine(() => SessionQueries.IsAtStationAfterLoop(1), "Build after loop 1");
            yield return _host.WaitForReportCoroutine(Steps.LANDED);
            Assert.IsEmpty(_host.LastReport.PoppedTargets, "The client still has popped balloons after the landing.");
            Assert.IsEmpty(_host.LastReport.BrokenRocks, "The client still has broken rocks after the landing.");
            _host.AssertSameView(false, true);
        }

        // The ship slows down at its acceleration limit after the throttle went to Stop.
        private IEnumerator WaitForShipStoppedCoroutine() {
            Vector3 last = Ship.transform.position;
            yield return _host.WaitForCoroutine(() => {
                float speed = (Ship.transform.position - last).magnitude / SPEED_SAMPLE_SECONDS;
                last = Ship.transform.position;
                return speed < STOPPED_SPEED;
            }, "the ship stopped in the cruise", _speedSample);
        }

        private static IEnumerator WaitForSightCoroutine(Func<bool> isInSight) {
            for (float waited = 0f; waited < SIGHT_WAIT_SECONDS && isInSight() == false; waited += Time.deltaTime)
                yield return null;
        }

        // Before the balloon at the height of the hull's middle, heading at it.
        private void PutShipBefore(FieldTargetDescription balloon) {
            Vector3 toTarget = LoopSmokeFlightDirection(balloon.Center);
            float hullMiddle = SessionQueries.Resolve<IShipRockImpactDebug>().DebugGetHullMiddleHeight();
            Vector3 position = balloon.Center - toTarget * (balloon.Radius + FLY_IN_DISTANCE) + Vector3.down * hullMiddle;
            Ship.FlightControlDebug.DebugMove(position - Ship.transform.position);
            Ship.FlightControlDebug.DebugFace(toTarget);
        }

        private Vector3 LoopSmokeFlightDirection(Vector3 to) =>
            Tests.PlayMode.LoopSmoke.LoopSmokeFlight.HorizontalDirection(Ship.transform.position, to);

        // The flight applies a debug move in its next step: a second move worked out in the same frame would add up.
        private IEnumerator PutShipBackCoroutine(Vector3 start) {
            Ship.FlightControlDebug.DebugMove(start - Ship.transform.position);
            yield return _physicsStep;
            yield return _physicsStep;
        }

        private ShipSocket FindTurretSocket() {
            foreach (ShipSocket socket in Ship.Sockets) {
                if (socket.SocketId == Steps.TURRET_SOCKET)
                    return socket;
            }

            throw new InvalidOperationException($"The ship has no socket {Steps.TURRET_SOCKET}.");
        }

        private static PlayerKey ClientKey() =>
            SessionQueries.Resolve<IPlayerIdentityService>().GetKey(CoopEconomyHostHarness.ClientConnection());

        // The pistol's shown model (its renderers) beside the client's body.
        private static float PistolDistanceFromHolder(GameObject pistol, NetworkIdentity holder) {
            Renderer model = pistol.GetComponentInChildren<Renderer>();
            return model.enabled ? Vector3.Distance(model.bounds.center, holder.GetComponent<GrabController>().ArmPoint.position) : float.MaxValue;
        }

        // A hand weapon's tracers fly in its feel frame, a scene root of their own: one shown means this peer replayed a
        // shot.
        private static bool HasActiveTracer(Scene scene, string frameName) {
            foreach (GameObject root in scene.GetRootGameObjects()) {
                if (root.name == frameName && root.GetComponentsInChildren<MeshRenderer>(false).Length > 0)
                    return true;
            }

            return false;
        }

        // A turret's tracers fly in its effects root on the ship (the deck feel frame), next to its casings.
        private static bool HasActiveTurretTracer(Transform ship) {
            foreach (Transform frameChild in ship) {
                if (frameChild.name != WEAPON_FEEL_EFFECTS)
                    continue;

                foreach (Transform effect in frameChild) {
                    if (effect.gameObject.activeSelf && effect.name.StartsWith(TRACER_PREFIX, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }
    }
}
#endif
