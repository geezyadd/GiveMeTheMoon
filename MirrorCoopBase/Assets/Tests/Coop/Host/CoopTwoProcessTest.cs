#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts;
using Features.GameCoreModule.Contracts;
using Features.PlayerLifeModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;
using Features.ShopModule.Scripts.Network;
using Features.ShopModule.Scripts.UI;
using Game.Connection;
using Mirror;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.Coop.Host {
    // The editor hosts, a development build of the game is the second player (CoopTestClient). The client joins by direct
    // IP, both ride one loop, the client leaves in flight and is turned away in flight, comes back at the next station on
    // the deck, dies there and is bought back by the host at the kiosk, and both ride one more loop. At every step the
    // host compares its own view with the client's report.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopTwoProcessTest {
        private const int TEST_TIMEOUT_MS = 420000;
        private const float STEP_TIMEOUT_SECONDS = 90f;
        private const float CLIENT_EXIT_TIMEOUT_SECONDS = 30f;
        private const float SETTLE_SECONDS = 1.5f;
        private const string LOOPBACK_ADDRESS = "127.0.0.1";
        private const string CLIENT_LOG_FILE = "client.log";
        private const string RUN_FOLDER_FORMAT = "yyyyMMdd-HHmmss";
        private const int LEAVE_LOOP = 2;
        private const int LAST_LOOP = 3;

        private readonly WaitForSeconds _settle = new WaitForSeconds(SETTLE_SECONDS);

        private LoopSmokeErrorLog _errorLog;
        private ShipRunConfig _fastConfig;
        private string _runDirectory;
        private CoopTestExchange _exchange;
        private CoopClientProcess _client;
        private CoopObservation _lastReport;
        private int _readReports;
        private float _testStarted;
        private float _stepStarted;
        private ShipBase _ship;
        private IReadOnlyShipRunModel _model;

        [SetUp]
        public void SetUp() {
            Assert.IsFalse(ProjectContext.HasInstance, "ProjectContext exists before the test: the config override cannot be bound.");
            _errorLog = new LoopSmokeErrorLog();
            ProjectContext.PostInstall += OnProjectContextPostInstall;
        }

        [TearDown]
        public void TearDown() {
            ProjectContext.PostInstall -= OnProjectContextPostInstall;
            _client?.Dispose();
            _errorLog.Dispose();
            DeletePassedRunDirectory();
            if (_fastConfig != null)
                Object.Destroy(_fastConfig);
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientJoinsByIp_AndLeavesInFlight_AndRejoinsAtStation_AndIsBoughtBack_ThenHostAndClientSeeTheSameLoop() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            _testStarted = Time.realtimeSinceStartup;
            yield return HostAndLetClientInCoroutine();
            yield return StartAndBoardCoroutine();
            yield return RideLoopTogetherCoroutine(1);
            yield return FlyWhileClientLeavesCoroutine();
            yield return LetClientRejoinCoroutine();
            yield return BuyBackDeadClientCoroutine();
            yield return RideLoopTogetherCoroutine(LAST_LOOP);
            yield return FinishCoroutine();
        }

        private IEnumerator HostAndLetClientInCoroutine() {
            BeginStep("host the lobby");
            ushort port = FindFreePort();
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return WaitForCoroutine(SessionQueries.IsMenuReady, "the menu");
            CoopSessionActions.PrepareDirect(port, CoopTestPlayers.HOST_NAME);
            CoopSessionActions.HostThroughMenu();
            yield return WaitForCoroutine(LoopSmokeHostSteps.IsLobbyReady, "the host's lobby");

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
            LoopSmokeHostSteps.ClickStart();
            yield return WaitForCoroutine(SessionQueries.IsGameAtStation, "GameScene at the start station");
            _ship = Object.FindAnyObjectByType<ShipBase>();
            _model = SessionQueries.Resolve<IReadOnlyShipRunModel>();
            LoopSmokeHostSteps.InstallRequiredEngines(_ship);
            ShipDeckBoarding.PutAboveDeck(SessionQueries.LocalPlayer.GetComponent<CharacterMovableBase>(), _ship);
            yield return _settle;
            Assert.IsTrue(CoopObserver.IsOnDeck(_ship, SessionQueries.LocalPlayer.transform.position), "The host did not stay on the deck.");
            yield return WaitForReportCoroutine(CoopTestSteps.BOARDED);
            yield return WaitForCoroutine(IsClientOnDeckOnServer, "the client on the deck by its server position");
            AssertSameView(false, true);
        }

        private IEnumerator RideLoopTogetherCoroutine(int loopIndex) {
            yield return LaunchAndCompareCruiseCoroutine(loopIndex);
            BeginStep($"loop {loopIndex} landing");
            yield return WaitForCoroutine(() => IsAtStationAfterLoop(loopIndex), $"Build after loop {loopIndex}");
            AssertLanded();
            yield return WaitForReportCoroutine(CoopTestSteps.LANDED);
            Assert.IsTrue(IsClientOnDeckOnServer(), $"The client is not on the deck by its server position after loop {loopIndex}: {ClientServerPosition()}.");
            AssertSameView(false, true);
        }

        private IEnumerator FlyWhileClientLeavesCoroutine() {
            yield return LaunchAndCompareCruiseCoroutine(LEAVE_LOOP);
            BeginStep("client leaves in flight");
            yield return WaitForReportCoroutine(CoopTestSteps.LEFT_IN_FLIGHT);
            yield return WaitForCoroutine(IsClientGone, "the client's connection gone on the server");
            Assert.AreNotEqual(ShipRunPhase.Build, _model.Phase, "The client left after the landing: the scenario cannot check a join in flight.");

            BeginStep("client knocks in flight");
            yield return WaitForReportCoroutine(CoopTestSteps.REJECTED_IN_FLIGHT);
            Assert.AreEqual(nameof(ConnectionRejectReason.InFlight), _lastReport.RejectReason, "A join in flight was turned away for another reason.");
            Assert.IsTrue(_lastReport.IsRejectTextShown, "The client's menu does not show the in-flight reason.");
            Assert.IsTrue(IsClientGone(), "The server kept a player who was turned away in flight.");

            BeginStep($"loop {LEAVE_LOOP} landing without the client");
            yield return WaitForCoroutine(() => IsAtStationAfterLoop(LEAVE_LOOP), $"Build after loop {LEAVE_LOOP}");
            AssertLanded();
        }

        private IEnumerator LetClientRejoinCoroutine() {
            BeginStep("client rejoins at the station");
            _exchange.AppendCommand(CoopTestSteps.COMMAND_REJOIN);
            yield return WaitForReportCoroutine(CoopTestSteps.REJOINED);
            Assert.IsTrue(IsClientPlayerSpawned(), "The rejoined client has no player on the server.");
            Assert.IsTrue(IsClientOnDeckOnServer(), $"The rejoined client is not on the deck by its server position: {ClientServerPosition()}.");
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
                yield return LoopSmokeShopCheck.OpenAtKioskCoroutine(FindKioskNearestToShip(), shop);
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
            ShipDeckBoarding.PutAboveDeck(SessionQueries.LocalPlayer.GetComponent<CharacterMovableBase>(), _ship);
            yield return _settle;
            Assert.IsFalse(shop.IsOpen, "The shop window stayed open after the host walked back to the deck.");
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
            CoopSessionActions.LeaveThroughHud();
            yield return WaitForCoroutine(SessionQueries.IsBackInMenu, "the menu after Leave");
            Debug.Log($"[CoopTest] the whole scenario took {Time.realtimeSinceStartup - _testStarted:0.0} s");
            Assert.AreEqual(0, _errorLog.Count, $"The host logged errors:\n{_errorLog.Describe()}");
        }

        private IEnumerator LaunchAndCompareCruiseCoroutine(int loopIndex) {
            BeginStep($"loop {loopIndex} takeoff");
            Assert.AreEqual(loopIndex - 1, _model.LoopIndex, "LoopIndex before launch.");
            LoopSmokeHostSteps.PullLaunchLever();
            yield return WaitForCoroutine(() => _model.Phase == ShipRunPhase.Cruise, $"Cruise of loop {loopIndex}");
            BeginStep($"loop {loopIndex} cruise");
            yield return WaitForReportCoroutine(CoopTestSteps.IN_CRUISE);
            Assert.AreEqual(ShipRunPhase.Cruise, _model.Phase, $"The client's cruise report of loop {loopIndex} came after the cruise.");
            AssertSameView(true, loopIndex != LEAVE_LOOP);
        }

        private void AssertLanded() {
            Assert.AreEqual(ShipRunAbortReason.None, _model.LastAbortReason, "The flight ended as a wreck.");
            Assert.AreEqual(LoopSmokeHostSteps.REQUIRED_ENGINES, LoopSmokeHostSteps.CountInstalledEngines(_ship), "Engines were lost on landing.");
            Assert.IsTrue(CoopObserver.IsOnDeck(_ship, SessionQueries.LocalPlayer.transform.position), "The host is not on the deck after landing.");
        }

        private void AssertSameView(bool isInFlight, bool isClientStaying) {
            CoopObservation host = CoopObserver.CaptureGame(_lastReport.Step);
            List<string> mismatches = CoopObservationComparison.FindMismatches(host, _lastReport, isInFlight, isClientStaying);
            Assert.IsEmpty(mismatches, $"Host and client differ at {_lastReport.Step}:\n{string.Join("\n", mismatches)}\n"
                                       + $"host: {host.Describe()}\nclient: {_lastReport.Describe()}");
        }

        private void BeginStep(string step) {
            float now = Time.realtimeSinceStartup;
            Debug.Log($"[CoopTest] {step} (at {now - _testStarted:0.0} s; the previous step took {now - _stepStarted:0.0} s)");
            _stepStarted = now;
        }

        // Takes the client's next report and fails at once when the client failed, quit or went silent.
        private IEnumerator WaitForReportCoroutine(string step) {
            _lastReport = null;
            yield return WaitForCoroutine(() => TryTakeReport(step), $"the client's '{step}' report");
            Debug.Log($"[CoopTest] client: {_lastReport.Describe()}");
        }

        private bool TryTakeReport(string step) {
            List<CoopObservation> reports = _exchange.ReadReports();
            if (reports.Count <= _readReports)
                return false;

            CoopObservation report = reports[_readReports];
            _readReports++;
            if (report.Step == CoopTestSteps.FAILED)
                Assert.Fail($"The client failed: {report.Message}\n{string.Join("\n", report.Errors)}\n{_client.DescribeLogTail()}");

            Assert.AreEqual(step, report.Step, $"The client reported another step than expected: {report.Describe()}");
            _lastReport = report;
            return true;
        }

        private IEnumerator WaitForCoroutine(Func<bool> condition, string target) {
            float deadline = Time.realtimeSinceStartup + STEP_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {STEP_TIMEOUT_SECONDS} s waiting for {target}. Host errors:\n{_errorLog.Describe()}\n{DescribeClient()}");

                if (_client != null && _client.HasExited)
                    Assert.Fail($"The client quit while the host waited for {target}.\n{_client.DescribeLogTail()}");

                yield return null;
            }
        }

        private string DescribeClient() =>
            _client == null ? "(no client started)" : _client.DescribeLogTail();

        private bool IsAtStationAfterLoop(int loopIndex) =>
            _model.Phase == ShipRunPhase.Build && _model.LoopIndex == loopIndex;

        private ShopKioskInteractable FindKioskNearestToShip() {
            ShopKioskInteractable nearest = null;
            float best = float.MaxValue;
            foreach (ShopKioskInteractable kiosk in Object.FindObjectsByType<ShopKioskInteractable>(FindObjectsSortMode.None)) {
                float distance = (kiosk.transform.position - _ship.transform.position).sqrMagnitude;
                if (distance >= best)
                    continue;

                best = distance;
                nearest = kiosk;
            }

            Assert.IsNotNull(nearest, "No shop kiosk in the scene.");
            return nearest;
        }

        private static PlayerLifeState ClientLifeState() {
            Assert.IsTrue(TryGetClientConnection(out NetworkConnectionToClient connection) && connection.identity != null, "The client has no player on the server.");
            return connection.identity.GetComponent<PlayerLifeBody>().LifeState;
        }

        private static bool IsClientPlayerSpawned() =>
            TryGetClientConnection(out NetworkConnectionToClient connection) && connection.identity != null;

        private static bool IsClientGone() =>
            TryGetClientConnection(out NetworkConnectionToClient _) == false;

        private bool IsClientOnDeckOnServer() =>
            IsClientPlayerSpawned() && CoopObserver.IsOnDeck(_ship, ClientServerPosition());

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

        private void OnProjectContextPostInstall() =>
            _fastConfig = LoopSmokeHostSteps.BindFastTimings(ProjectContext.Instance.Container);
    }
}
#endif
