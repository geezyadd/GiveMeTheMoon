#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Features.CameraModule.Scripts;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;
using Features.ShopModule.Scripts.Network;
using Features.ShopModule.Scripts.UI;
using Features.WeaponModule.Scripts.Network;
using Game.Connection;
using Mirror;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.Coop.Host {
    // The editor hosts, a development build of the game is the second player (CoopTestClient). The client joins by direct
    // IP, both ride one loop (the host navigates at the radar's sonar console and marks the station, the client must see
    // the mark in its synced model), the client takes the helm at the station and steers the next cruise, leaves in flight
    // from the helm (the seat is free again) and is turned away in flight, comes back at the next station on the deck and
    // not at the helm, dies there and is bought back by the host at the kiosk, then launches the last loop by pushing the
    // throttle handle (the host sees it held by the client's key, at Full and the ship flying); in that cruise the
    // client pulls the handle back to Stop (the ship stops and stays still) and pushes it to Full again (it flies
    // again), and both ride it. At every step the host compares its own view with the client's report. An item the host drops on the deck at the
    // first station lies on the deck in every cruise, the same on both peers.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopTwoProcessTest {
        private const int TEST_TIMEOUT_MS = 420000;
        private const float STEP_TIMEOUT_SECONDS = 90f;
        private const float CLIENT_EXIT_TIMEOUT_SECONDS = 30f;
        private const string LOOPBACK_ADDRESS = "127.0.0.1";
        private const string CLIENT_LOG_FILE = "client.log";
        private const string RUN_FOLDER_FORMAT = "yyyyMMdd-HHmmss";
        private const int LEAVE_LOOP = 2;
        private const int LAST_LOOP = 3;
        private const float SIT_POINT_TOLERANCE = 0.3f;
        // The client holds D from the helm into the cruise: the ship turns right at the flight's turn rate.
        private const float MIN_STEERED_DEGREES = 1f;
        private const float FULL_THROTTLE = 1f;
        private const string HAND_WEAPON_FEEL_FRAME = "HandWeaponFeelFrame";
        private const float THROTTLE_TOLERANCE = 0.02f;
        // The client's throttle at Full flies the ship at least this fast in the short test cruise (m/s).
        private const float MIN_CRUISE_SPEED = 5f;
        private const float SPEED_SAMPLE_SECONDS = 0.25f;
        // The client's pull to Stop brings the ship to rest within this long at the acceleration limit (s).
        private const float STOP_TIMEOUT_SECONDS = 6f;
        private const float STAY_STOPPED_SECONDS = 2f;
        private const float STOPPED_SPEED = 0.2f;

        private readonly WaitForSeconds _exchangePoll = new WaitForSeconds(CoopTestExchange.POLL_SECONDS);
        private readonly WaitForSeconds _speedSample = new WaitForSeconds(SPEED_SAMPLE_SECONDS);
        private readonly WaitForSeconds _stayStopped = new WaitForSeconds(STAY_STOPPED_SECONDS);

        private LoopSmokeErrorLog _errorLog;
        private LoopSmokeShipRunConfigOverride _configOverride;
        private LoopSmokeSession _session;
        private LoopSmokeFlight _flight;
        private LoopSmokeSonarCheck _sonar;
        private string _runDirectory;
        private CoopTestExchange _exchange;
        private CoopClientProcess _client;
        private CoopObservation _lastReport;
        private Grabbable _deckItem;
        private int _readReports;
        private float _testStarted;
        private float _stepStarted;
        private float _cruiseStartYaw;
        private Vector3 _speedSampleStart;
        private float _speedSampleStartTime;

        private ShipBase Ship =>
            _session.Ship;

        private IReadOnlyShipRunModel Model =>
            _session.Model;

        [SetUp]
        public void SetUp() {
            _configOverride = new LoopSmokeShipRunConfigOverride(LoopSmokeShipRunConfigOverride.FAST_TIMINGS_JSON);
            _errorLog = new LoopSmokeErrorLog();
            _session = new LoopSmokeSession(_errorLog);
        }

        [TearDown]
        public void TearDown() {
            _client?.Dispose();
            _errorLog.Dispose();
            DeletePassedRunDirectory();
            _configOverride.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientJoinsByIp_AndLeavesInFlight_AndRejoinsAtStation_AndIsBoughtBack_ThenHostAndClientSeeTheSameLoop() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            _testStarted = Time.realtimeSinceStartup;
            yield return HostAndLetClientInCoroutine();
            yield return StartAndBoardCoroutine();
            yield return RideLoopTogetherCoroutine(1);
            yield return WatchClientTakeHelmCoroutine();
            yield return FlyWhileClientLeavesCoroutine();
            yield return LetClientRejoinCoroutine();
            yield return BuyBackDeadClientCoroutine();
            yield return WatchClientFirePistolCoroutine();
            yield return RideLoopTogetherCoroutine(LAST_LOOP);
            yield return FinishCoroutine();
        }

        private IEnumerator HostAndLetClientInCoroutine() {
            BeginStep("host the lobby");
            ushort port = FindFreePort();
            yield return _session.EnterLobbyAsHostCoroutine(() => CoopSessionActions.PrepareDirect(port, CoopTestPlayers.HOST_NAME));

            BeginStep("client joins the lobby");
            _runDirectory = Path.Combine(CoopClientBuildPaths.RunsDirectory, DateTime.Now.ToString(RUN_FOLDER_FORMAT));
            Directory.CreateDirectory(_runDirectory);
            _exchange = new CoopTestExchange(_runDirectory);
            CoopTestArguments arguments = new CoopTestArguments(LOOPBACK_ADDRESS, port, CoopTestSteps.SCENARIO_LEAVE_AND_REJOIN, _runDirectory);
            _client = CoopClientProcess.Start(arguments, Path.Combine(_runDirectory, CLIENT_LOG_FILE));
            yield return WaitForReportCoroutine(CoopTestSteps.IN_LOBBY);
            yield return WaitForCoroutine(IsClientPlayerSpawned, "the client's player on the server");
        }

        private IEnumerator StartAndBoardCoroutine() {
            BeginStep("start the game and board");
            yield return _session.StartGameCoroutine();
            _configOverride.AssertBound();
            _flight = new LoopSmokeFlight(_session, new LoopSmokeFieldCheck(ProjectContext.Instance.Container));
            _flight.InstallRequiredEngines();
            _sonar = new LoopSmokeSonarCheck(_session);
            _sonar.InstallRadarAtStation();
            yield return DropItemOnDeckCoroutine();
            yield return _flight.BoardPlayerCoroutine();
            yield return _sonar.StandByConsoleCoroutine();
            yield return WaitForReportCoroutine(CoopTestSteps.BOARDED);
            yield return WaitForCoroutine(IsClientOnDeckOnServer, "the client on the deck by its server position");
            AssertSameView(false, true);
        }

        private IEnumerator RideLoopTogetherCoroutine(int loopIndex) {
            yield return LaunchAndCompareCruiseCoroutine(loopIndex);
            if (loopIndex == CoopTestSteps.SONAR_MARK_LOOP)
                yield return MarkForClientCoroutine();

            BeginStep($"loop {loopIndex} landing");
            yield return WaitForCoroutine(() => SessionQueries.IsAtStationAfterLoop(loopIndex), $"Build after loop {loopIndex}");
            AssertLanded();
            yield return WaitForReportCoroutine(CoopTestSteps.LANDED);
            Assert.IsTrue(IsClientOnDeckOnServer(), $"The client is not on the deck by its server position after loop {loopIndex}: {ClientServerPosition()}.");
            AssertSameView(false, true);
            // The console stands at the deck's edge: the host goes back to its boarding spot for the next loops.
            if (loopIndex == CoopTestSteps.SONAR_MARK_LOOP)
                yield return _flight.BoardPlayerCoroutine();
        }

        // The seat is keyed by the client's player key on the server, and the host sees the client's body in it.
        private IEnumerator WatchClientTakeHelmCoroutine() {
            BeginStep("client takes the helm at the station");
            yield return WaitForReportCoroutine(CoopTestSteps.SEATED_AT_HELM);
            Assert.IsTrue(_lastReport.IsHelmPilot, "The client does not steer from the helm on its own peer.");
            ShipSocket helm = CoopHelm.FindSocket(Ship);
            ShipRider client = ClientRider();
            yield return WaitForCoroutine(() => helm.IsHeldBy(client.Key), "the helm held by the client's key on the server");
            Assert.IsTrue(client.IsSeated, "The client's rider is not seated on the server.");
            Assert.IsTrue(CoopHelm.FindStation(Ship).HasPilot, "The host's helm station sees no pilot.");
            Assert.IsFalse(CoopHelm.FindStation(Ship).IsSteeredByLocalPlayer, "The host's helm station takes the client for the host.");
            float sitDistance = Vector3.Distance(client.transform.position, helm.Seat.SitPoint.position);
            Assert.Less(sitDistance, SIT_POINT_TOLERANCE, $"The host does not see the client at the helm's seat: {sitDistance:0.00} m away.");
        }

        private IEnumerator FlyWhileClientLeavesCoroutine() {
            yield return LaunchAndCompareCruiseCoroutine(LEAVE_LOOP);
            Assert.IsTrue(_lastReport.IsHelmPilot, "The client is not at the helm in the cruise.");
            yield return WaitForCoroutine(IsShipSteeredRight, "the ship turning right under the client pilot's D");
            BeginStep("client leaves in flight");
            yield return WaitForReportCoroutine(CoopTestSteps.LEFT_IN_FLIGHT);
            yield return WaitForCoroutine(IsClientGone, "the client's connection gone on the server");
            Assert.AreNotEqual(ShipRunPhase.Build, Model.Phase, "The client left after the landing: the scenario cannot check a join in flight.");
            Assert.IsFalse(CoopHelm.FindSocket(Ship).HasOccupant, "The pilot left, but the helm is still taken.");
            Assert.IsFalse(CoopHelm.FindStation(Ship).HasPilot, "The pilot left, but the helm station still has a pilot.");
            // The client steered away from the destination; with nobody at the helm the autopilot keeps that heading.
            _flight.FaceDestination();

            BeginStep("client knocks in flight");
            yield return WaitForReportCoroutine(CoopTestSteps.REJECTED_IN_FLIGHT);
            Assert.AreEqual(nameof(ConnectionRejectReason.InFlight), _lastReport.RejectReason, "A join in flight was turned away for another reason.");
            Assert.IsTrue(_lastReport.IsRejectTextShown, "The client's menu does not show the in-flight reason.");
            Assert.IsTrue(IsClientGone(), "The server kept a player who was turned away in flight.");

            BeginStep($"loop {LEAVE_LOOP} landing without the client");
            yield return WaitForCoroutine(() => SessionQueries.IsAtStationAfterLoop(LEAVE_LOOP), $"Build after loop {LEAVE_LOOP}");
            AssertLanded();
        }

        private IEnumerator LetClientRejoinCoroutine() {
            BeginStep("client rejoins at the station");
            _exchange.AppendCommand(CoopTestSteps.COMMAND_REJOIN);
            yield return WaitForReportCoroutine(CoopTestSteps.REJOINED);
            Assert.IsTrue(IsClientPlayerSpawned(), "The rejoined client has no player on the server.");
            Assert.IsTrue(IsClientOnDeckOnServer(), $"The rejoined client is not on the deck by its server position: {ClientServerPosition()}.");
            Assert.IsFalse(ClientRider().IsSeated, "The rejoined client is seated on the server.");
            Assert.IsFalse(CoopHelm.FindSocket(Ship).HasOccupant, "The helm is taken after the old pilot came back.");
            Assert.IsFalse(_lastReport.IsHelmPilot, "The rejoined client is back at the helm.");
            AssertSameView(false, true);
        }

        private IEnumerator BuyBackDeadClientCoroutine() {
            BeginStep("client dies at the station");
            yield return WaitForReportCoroutine(CoopTestSteps.DIED);
            Assert.AreEqual(nameof(PlayerLifeState.Dead), _lastReport.LifeState, "The client does not see itself dead.");
            Assert.IsTrue(_lastReport.IsSpectating, "The dead client does not spectate.");
            yield return WaitForCoroutine(() => ClientLifeState() == PlayerLifeState.Dead, "the client dead on the server");

            BeginStep("host buys the client back at the kiosk");
            ShopModel shop = SessionQueries.Resolve<ShopModel>();
            IReadOnlyWalletModel wallet = SessionQueries.Resolve<IReadOnlyWalletModel>();
            long balance = wallet.Balance;
            try {
                CameraLookDriver.DebugPitchOverride = true;
                yield return LoopSmokeShopCheck.OpenAtKioskCoroutine(FindStationKiosk(), shop);
                yield return WaitForCoroutine(() => LoopSmokeReviveCheck.FindCrewRows().Count == 1, "the client in the shop's Crew section");
                CrewReviveRow row = LoopSmokeReviveCheck.FindCrewRows()[0];
                Assert.IsTrue(LoopSmokeReviveCheck.HasLabel(row, CoopTestPlayers.CLIENT_NAME), $"The Crew row does not show {CoopTestPlayers.CLIENT_NAME}.");
                row.GetComponentInChildren<Button>().onClick.Invoke();
                yield return WaitForReportCoroutine(CoopTestSteps.REVIVED);
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            Assert.AreEqual(nameof(PlayerLifeState.Alive), _lastReport.LifeState, "The bought-back client does not see itself alive.");
            Assert.IsFalse(_lastReport.IsSpectating, "The bought-back client still spectates.");
            Assert.AreEqual(PlayerLifeState.Alive, ClientLifeState(), "The bought-back client is not alive on the server.");
            Assert.AreEqual(balance - SessionQueries.Resolve<ReviveConfiguration>().RevivePrice, wallet.Balance, "The buy-back did not take the price once.");
            Assert.IsTrue(IsClientOnDeckOnServer(), $"The bought-back client is not on the deck by its server position: {ClientServerPosition()}.");
            AssertSameView(false, true);

            BeginStep("host walks back to the deck");
            yield return _flight.BoardPlayerCoroutine();
            Assert.IsFalse(shop.IsOpen, "The shop window stayed open after the host walked back to the deck.");
        }

        // The host puts a pistol into the client's hand (test setup: the purchase is LoopSmokePistolCheck's) and a debug
        // dummy before the ship; the client aims and clicks. The host sees the client's trigger arrive, replays the
        // shot with its tracer on the client's pistol, and the server accepts the client's hit on the dummy.
        private IEnumerator WatchClientFirePistolCoroutine() {
            BeginStep("client fires the pistol");
            ShipRider client = ClientRider();
            HandWeaponDefinition pistol = SessionQueries.Resolve<ShopCatalog>().HandWeapons[0];
            GameObject item = Object.Instantiate(pistol.ItemPrefab.gameObject, client.transform.position + Vector3.up, client.transform.rotation);
            NetworkServer.Spawn(item);
            yield return null;
            Assert.IsTrue(client.GetComponent<GrabController>().ServerGive(item.GetComponent<Grabbable>()), "The pistol could not be put into the client's hand.");
            IWeaponDebugTargets targets = SessionQueries.Resolve<IWeaponDebugTargets>();
            WeaponDebugTarget target = targets.DebugSpawnTarget();
            Assert.IsNotNull(target, "The debug target did not spawn at the station.");

            _exchange.AppendCommand(CoopTestSteps.COMMAND_FIRE_PISTOL);
            WeaponHolderSync clientWeapon = client.GetComponent<WeaponHolderSync>();
            bool hasSeenTrigger = false;
            bool hasSeenTracer = false;
            _lastReport = null;
            yield return WaitForCoroutine(() => WatchPistolAndTakeReport(clientWeapon, item.scene, ref hasSeenTrigger, ref hasSeenTracer), "the client's 'pistol-fired' report");
            Assert.IsTrue(hasSeenTrigger, "The host never saw the client's pistol trigger.");
            Assert.IsTrue(hasSeenTracer, "The host did not replay the client's pistol shot (no tracer).");
            Assert.GreaterOrEqual(target.Hits, 1, "The server accepted no hit of the client's pistol on the dummy.");
            Debug.Log($"[CoopTest] the client's pistol: {target.Hits} accepted hits on the dummy, its trigger and a tracer seen on the host");

            client.GetComponent<GrabController>().ServerConsumeHeld();
            targets.DebugDespawnTarget();
        }

        private bool WatchPistolAndTakeReport(WeaponHolderSync clientWeapon, UnityEngine.SceneManagement.Scene scene, ref bool hasSeenTrigger, ref bool hasSeenTracer) {
            hasSeenTrigger |= clientWeapon.IsTriggerHeld;
            hasSeenTracer |= HasActiveTracer(scene);
            return TryTakeReport(CoopTestSteps.PISTOL_FIRED);
        }

        // A hand weapon's tracers fly in its feel frame, a scene root of their own: one shown means this peer
        // replayed a shot.
        private static bool HasActiveTracer(UnityEngine.SceneManagement.Scene scene) {
            foreach (GameObject root in scene.GetRootGameObjects()) {
                if (root.name != HAND_WEAPON_FEEL_FRAME)
                    continue;

                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(false)) {
                    if (renderer.gameObject.activeInHierarchy)
                        return true;
                }
            }

            return false;
        }

        private IEnumerator FinishCoroutine() {
            BeginStep("client leaves and quits");
            _exchange.AppendCommand(CoopTestSteps.COMMAND_FINISH);
            yield return WaitForReportCoroutine(CoopTestSteps.FINISHED);
            Assert.IsEmpty(_lastReport.Errors, $"The client logged errors:\n{string.Join("\n", _lastReport.Errors)}");
            float deadline = Time.realtimeSinceStartup + CLIENT_EXIT_TIMEOUT_SECONDS;
            while (_client.HasExited == false && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.IsTrue(_client.HasExited, "The client did not quit after its last step.");
            _client.Dispose();
            _client = null;
            yield return WaitForCoroutine(IsClientGone, "the client's connection gone on the server");

            BeginStep("host leaves to the menu");
            yield return _session.LeaveToMenuCoroutine();
            Debug.Log($"[CoopTest] the whole scenario took {Time.realtimeSinceStartup - _testStarted:0.0} s");
            Assert.AreEqual(0, _errorLog.Count, $"The host logged errors:\n{_errorLog.Describe()}");
        }

        private IEnumerator LaunchAndCompareCruiseCoroutine(int loopIndex) {
            BeginStep($"loop {loopIndex} takeoff");
            Assert.AreEqual(loopIndex - 1, Model.LoopIndex, "LoopIndex before launch.");
            if (loopIndex == CoopTestSteps.THROTTLE_LOOP)
                yield return WatchClientPushThrottleCoroutine();
            else
                _flight.PullLaunchLever(loopIndex);

            yield return WaitForCoroutine(() => Model.Phase == ShipRunPhase.Cruise, $"Cruise of loop {loopIndex}");
            FaceForCruise(loopIndex);
            // The ship takes the faced heading in its LateUpdate.
            yield return null;
            _cruiseStartYaw = Ship.transform.eulerAngles.y;
            BeginStep($"loop {loopIndex} cruise");
            yield return WaitForReportCoroutine(CoopTestSteps.IN_CRUISE);
            Assert.AreEqual(ShipRunPhase.Cruise, Model.Phase, $"The client's cruise report of loop {loopIndex} came after the cruise.");
            AssertSameView(true, loopIndex != LEAVE_LOOP);
            AssertDeckItemLiesOnDeck(loopIndex);
            if (loopIndex != CoopTestSteps.THROTTLE_LOOP)
                yield break;

            yield return WaitForCoroutine(IsShipCruising, "the ship flying at the client's throttle");
            yield return WatchClientStopAndGoCoroutine();
        }

        // The client holds the handle in cruise: its pull to Stop stops the ship on the server, its push flies it again.
        private IEnumerator WatchClientStopAndGoCoroutine() {
            BeginStep("client pulls the throttle handle to Stop in cruise");
            IReadOnlyShipThrottleModel throttle = SessionQueries.Resolve<IReadOnlyShipThrottleModel>();
            _exchange.AppendCommand(CoopTestSteps.COMMAND_PULL_THROTTLE);
            yield return WaitForReportCoroutine(CoopTestSteps.THROTTLE_PULLED);
            Assert.AreEqual(0f, throttle.Position, THROTTLE_TOLERANCE, "The host does not see the throttle at Stop where the client pulled it in cruise.");
            Assert.AreEqual(_lastReport.ThrottlePosition, throttle.Position, THROTTLE_TOLERANCE, "Host and client see the throttle at different positions in cruise.");
            yield return WaitForShipStoppedCoroutine();
            Vector3 stoppedAt = Ship.transform.position;
            yield return _stayStopped;
            Assert.Less(HorizontalDistance(stoppedAt, Ship.transform.position) / STAY_STOPPED_SECONDS, STOPPED_SPEED, "The ship did not stay still at the client's Stop.");
            Assert.AreEqual(ShipRunPhase.Cruise, Model.Phase, "The stopped ship left the cruise.");

            BeginStep("client pushes the throttle handle to Full in cruise");
            _exchange.AppendCommand(CoopTestSteps.COMMAND_PUSH_THROTTLE_IN_CRUISE);
            yield return WaitForReportCoroutine(CoopTestSteps.THROTTLE_PUSHED_IN_CRUISE);
            Assert.AreEqual(FULL_THROTTLE, throttle.Position, THROTTLE_TOLERANCE, "The host does not see the throttle at Full where the client pushed it in cruise.");
            _speedSampleStart = Ship.transform.position;
            _speedSampleStartTime = Time.time;
            yield return WaitForCoroutine(IsShipCruising, "the ship flying again at the client's Full");
            _flight.FaceDestination();
        }

        // The short test cruise would reach the station while the client works the throttle: that cruise first heads
        // away from it and turns to it once the client is done.
        private void FaceForCruise(int loopIndex) {
            if (loopIndex == CoopTestSteps.THROTTLE_LOOP)
                Ship.FlightControlDebug.DebugFace(Ship.transform.position - Model.TransitDestination);
            else
                _flight.FaceDestination();
        }

        private IEnumerator WaitForShipStoppedCoroutine() {
            float deadline = Time.time + STOP_TIMEOUT_SECONDS;
            while (true) {
                Vector3 start = Ship.transform.position;
                float startTime = Time.time;
                yield return _speedSample;
                float speed = HorizontalDistance(start, Ship.transform.position) / (Time.time - startTime);
                if (speed < STOPPED_SPEED)
                    yield break;

                if (Time.time > deadline)
                    Assert.Fail($"The ship did not stop within {STOP_TIMEOUT_SECONDS} s of the client's pull to Stop: it moves at {speed:0.00} m/s, the server's throttle at {SessionQueries.Resolve<IReadOnlyShipThrottleModel>().Position:0.000}.");
            }
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to) {
            Vector3 moved = to - from;
            moved.y = 0f;
            return moved.magnitude;
        }

        // The client takes the handle by its player key on the server and pushes it past the launch mark to Full.
        private IEnumerator WatchClientPushThrottleCoroutine() {
            BeginStep("client pushes the throttle handle to take off");
            _exchange.AppendCommand(CoopTestSteps.COMMAND_PUSH_THROTTLE);
            IReadOnlyShipThrottleModel throttle = SessionQueries.Resolve<IReadOnlyShipThrottleModel>();
            yield return WaitForCoroutine(() => throttle.HolderKey == ClientRider().Key.Id, "the throttle handle held by the client's key on the server");
            yield return WaitForReportCoroutine(CoopTestSteps.THROTTLE_PUSHED);
            Assert.AreNotEqual(ShipRunPhase.Build, Model.Phase, "The client's push on the throttle handle did not take off on the host.");
            Assert.AreEqual(FULL_THROTTLE, throttle.Position, THROTTLE_TOLERANCE, "The host does not see the throttle at Full where the client left it.");
            Assert.AreEqual(_lastReport.ThrottlePosition, throttle.Position, THROTTLE_TOLERANCE, "Host and client see the throttle at different positions.");
            Assert.IsTrue(string.IsNullOrEmpty(throttle.HolderKey), "The handle is still held on the server after the client let go.");
            _speedSampleStart = Ship.transform.position;
            _speedSampleStartTime = Time.time;
        }

        // The host is the navigator: its station mark must reach the client's synced sonar model.
        private IEnumerator MarkForClientCoroutine() {
            BeginStep("host navigator marks the station");
            yield return _sonar.MarkStationInCruiseCoroutine();
            yield return WaitForReportCoroutine(CoopTestSteps.SONAR_MARK_SEEN);
            CollectionAssert.Contains(_lastReport.SonarMarks, LoopSmokeSonarCheck.STATION_CONTACT_ID, "The client does not see the host navigator's station mark.");
        }

        private void AssertLanded() {
            Assert.AreEqual(ShipRunAbortReason.None, Model.LastAbortReason, "The flight ended as a wreck.");
            Assert.AreEqual(LoopSmokeFlight.REQUIRED_ENGINES, _flight.CountInstalledEngines(), "Engines were lost on landing.");
            Vector3 host = SessionQueries.LocalPlayer.transform.position;
            Assert.IsTrue(SessionQueries.IsOnDeck(Ship, host), $"The host is not on the deck after landing: at {host}, deck {Ship.DeckGeometry.Bounds}.");
        }

        private void AssertSameView(bool isInFlight, bool isClientStaying) {
            CoopObservation host = CoopObserver.CaptureGame(_lastReport.Step);
            List<string> mismatches = CoopObservationComparison.FindMismatches(host, _lastReport, isInFlight, isClientStaying);
            Assert.IsEmpty(mismatches, $"Host and client differ at {_lastReport.Step}:\n{string.Join("\n", mismatches)}\n"
                                       + $"host: {host.Describe()}\nclient: {_lastReport.Describe()}");
        }

        private IEnumerator DropItemOnDeckCoroutine() {
            BeginStep("host drops an item on the deck");
            yield return LoopSmokeItemDelivery.DeliverModulesCoroutine(Ship, 1);
            Grabbable[] items = LoopSmokeDeckItemCheck.FindLooseBoxItems(Ship);
            Assert.IsNotEmpty(items, "No loose item at the first station to drop on the deck.");
            yield return LoopSmokeDeckItemCheck.DropOnDeckByHandCoroutine(Ship, items, OnDeckItemDropped);
        }

        private void OnDeckItemDropped(Grabbable item) =>
            _deckItem = item;

        // The cruise comparison holds the client's gap to the host's; here the host's own one is on the deck surface.
        private void AssertDeckItemLiesOnDeck(int loopIndex) {
            Assert.IsTrue(SessionQueries.IsOnDeck(Ship, _deckItem.transform.position), $"Loop {loopIndex}: the dropped {_deckItem.name} is off the deck.");
            CollectionAssert.Contains(_lastReport.DeckItemIds, _deckItem.netId, $"Loop {loopIndex}: the client does not see the dropped {_deckItem.name} on the deck.");
            float gap = ShipDeckItemRest.MeasureGapAboveDeck(Ship, _deckItem.GetComponentInChildren<BoxCollider>());
            Assert.Less(Mathf.Abs(gap), CoopObservationComparison.DECK_ITEM_GAP_TOLERANCE, $"Loop {loopIndex}: the dropped {_deckItem.name} does not lie on the deck: {gap:0.000} m above it.");
        }

        private void BeginStep(string step) {
            float now = Time.realtimeSinceStartup;
            Debug.Log($"[CoopTest] {step} (at {now - _testStarted:0.0} s; the previous step took {now - _stepStarted:0.0} s)");
            _stepStarted = now;
        }

        // Takes the client's next report and fails at once when the client failed, quit or went silent.
        private IEnumerator WaitForReportCoroutine(string step) {
            _lastReport = null;
            yield return WaitForCoroutine(() => TryTakeReport(step), $"the client's '{step}' report", _exchangePoll);
            Debug.Log($"[CoopTest] client: {_lastReport.Describe()}");
        }

        private bool TryTakeReport(string step) {
            List<CoopObservation> reports = _exchange.ReadReportsFrom(_readReports);
            if (reports.Count == 0)
                return false;

            CoopObservation report = reports[0];
            _readReports++;
            if (report.Step == CoopTestSteps.FAILED)
                Assert.Fail($"The client failed: {report.Message}\n{string.Join("\n", report.Errors)}\n{_client.DescribeLogTail()}");

            Assert.AreEqual(step, report.Step, $"The client reported another step than expected: {report.Describe()}");
            _lastReport = report;
            return true;
        }

        // pause: what to wait between checks; null checks every frame.
        private IEnumerator WaitForCoroutine(Func<bool> condition, string target, YieldInstruction pause = null) {
            float deadline = Time.realtimeSinceStartup + STEP_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {STEP_TIMEOUT_SECONDS} s waiting for {target}. Host errors:\n{_errorLog.Describe()}\n{DescribeClient()}");

                if (_client != null && _client.HasExited)
                    Assert.Fail($"The client quit while the host waited for {target}.\n{_client.DescribeLogTail()}");

                yield return pause;
            }
        }

        private string DescribeClient() =>
            _client == null ? "(no client started)" : _client.DescribeLogTail();

        private ShopKioskInteractable FindStationKiosk() {
            ShopKioskInteractable kiosk = _flight.FindPadUnderShip().GetComponentInChildren<ShopKioskInteractable>();
            Assert.IsNotNull(kiosk, "The station has no shop kiosk.");
            return kiosk;
        }

        private static PlayerLifeState ClientLifeState() {
            Assert.IsTrue(TryGetClientConnection(out NetworkConnectionToClient connection) && connection.identity != null, "The client has no player on the server.");
            return connection.identity.GetComponent<PlayerLifeBody>().LifeState;
        }

        // The ship's horizontal speed over the last sample on the host, restarted every sample.
        private bool IsShipCruising() {
            float elapsed = Time.time - _speedSampleStartTime;
            if (elapsed < SPEED_SAMPLE_SECONDS)
                return false;

            Vector3 moved = Ship.transform.position - _speedSampleStart;
            moved.y = 0f;
            _speedSampleStart = Ship.transform.position;
            _speedSampleStartTime = Time.time;
            return moved.magnitude / elapsed >= MIN_CRUISE_SPEED;
        }

        private bool IsShipSteeredRight() =>
            Mathf.DeltaAngle(_cruiseStartYaw, Ship.transform.eulerAngles.y) > MIN_STEERED_DEGREES;

        private static ShipRider ClientRider() {
            Assert.IsTrue(TryGetClientConnection(out NetworkConnectionToClient connection) && connection.identity != null, "The client has no player on the server.");
            return connection.identity.GetComponent<ShipRider>();
        }

        private static bool IsClientPlayerSpawned() =>
            TryGetClientConnection(out NetworkConnectionToClient connection) && connection.identity != null;

        private static bool IsClientGone() =>
            TryGetClientConnection(out NetworkConnectionToClient _) == false;

        private bool IsClientOnDeckOnServer() =>
            IsClientPlayerSpawned() && SessionQueries.IsOnDeck(Ship, ClientServerPosition());

        private static Vector3 ClientServerPosition() =>
            TryGetClientConnection(out NetworkConnectionToClient connection) && connection.identity != null
                ? connection.identity.transform.position
                : Vector3.positiveInfinity;

        private static bool TryGetClientConnection(out NetworkConnectionToClient connection) {
            foreach (NetworkConnectionToClient candidate in NetworkServer.connections.Values) {
                if (candidate != NetworkServer.localConnection) {
                    connection = candidate;
                    return true;
                }
            }

            connection = null;
            return false;
        }

        // A passed run leaves nothing behind; a failed one keeps the client's log and the exchange files for a look.
        private void DeletePassedRunDirectory() {
            bool isPassed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed;
            if (isPassed && _runDirectory != null && Directory.Exists(_runDirectory))
                Directory.Delete(_runDirectory, true);
        }

        private static ushort FindFreePort() {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            ushort port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
#endif
