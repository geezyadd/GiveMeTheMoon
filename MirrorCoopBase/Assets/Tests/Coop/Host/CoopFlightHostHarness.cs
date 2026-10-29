#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using Zenject;

namespace Tests.Coop.Host {
    // The host side of a coop flight test (the client runs a CoopClientScenarios scenario; CoopHostHarness drives the connection tests): hosts a direct-IP lobby, starts the client build with a scenario, starts
    // the game with the required engines, reads the client's reports, sends commands, compares the host's view with
    // the client's, finishes the session and cleans up. Failure messages carry the host's errors and the client's log.
    public sealed class CoopFlightHostHarness : IDisposable {
        private const float STEP_TIMEOUT_SECONDS = 90f;
        private const float CLIENT_EXIT_TIMEOUT_SECONDS = 30f;
        private const string LOOPBACK_ADDRESS = "127.0.0.1";
        private const string CLIENT_LOG_FILE = "client.log";
        private const string RUN_FOLDER_FORMAT = "yyyyMMdd-HHmmss";
        private const string SCREENSHOTS_FOLDER = "Builds/CoopScreenshots";
        private const string SCREENSHOT_PATTERN = "*.png";
        private const float STEADY_HEADING_SECONDS = 0.5f;
        private const float STEADY_YAW_DEGREES_PER_FRAME = 0.05f;

        private readonly WaitForSeconds _exchangePoll = new WaitForSeconds(CoopTestExchange.POLL_SECONDS);
        private readonly LoopSmokeShipRunConfigOverride _configOverride;
        private string _runDirectory;
        private CoopTestExchange _exchange;
        private CoopClientProcess _client;
        private int _readReports;
        private Action _everyFrame;
        private float _testStarted;
        private float _stepStarted;

        public LoopSmokeErrorLog ErrorLog { get; }
        public LoopSmokeSession Session { get; }
        public LoopSmokeFlight Flight { get; private set; }
        public CoopObservation LastReport { get; private set; }

        public ShipBase Ship =>
            Session.Ship;

        public IReadOnlyShipRunModel Model =>
            Session.Model;

        public CoopFlightHostHarness() {
            _configOverride = new LoopSmokeShipRunConfigOverride(LoopSmokeShipRunConfigOverride.FAST_TIMINGS_JSON);
            ErrorLog = new LoopSmokeErrorLog();
            Session = new LoopSmokeSession(ErrorLog);
        }

        // Keeps the client's screenshots of the test (they are copied out of the run folder, which a passed run deletes).
        public void Dispose() {
            _client?.Dispose();
            ErrorLog.Dispose();
            KeepScreenshots();
            DeletePassedRunDirectory();
            _configOverride.Dispose();
        }

        public IEnumerator HostAndLetClientInCoroutine(string scenario) {
            _testStarted = Time.realtimeSinceStartup;
            BeginStep("host the lobby");
            ushort port = FindFreePort();
            yield return Session.EnterLobbyAsHostCoroutine(() => CoopSessionActions.PrepareDirect(port, CoopTestPlayers.HOST_NAME));

            BeginStep($"client joins the lobby ({scenario})");
            _runDirectory = Path.Combine(CoopClientBuildPaths.RunsDirectory, DateTime.Now.ToString(RUN_FOLDER_FORMAT));
            Directory.CreateDirectory(_runDirectory);
            _exchange = new CoopTestExchange(_runDirectory);
            CoopTestArguments arguments = new CoopTestArguments(LOOPBACK_ADDRESS, port, scenario, _runDirectory);
            _client = CoopClientProcess.Start(arguments, Path.Combine(_runDirectory, CLIENT_LOG_FILE));
            yield return WaitForReportCoroutine(CoopTestSteps.IN_LOBBY);
            yield return WaitForCoroutine(IsClientPlayerSpawned, "the client's player on the server");
        }

        // Starts the run with the required engines, boards the host and waits until the client boarded too.
        // atStation: a step at the start station before anyone boards (none by default).
        public IEnumerator StartAndBoardCoroutine(Func<IEnumerator> atStation = null) {
            BeginStep("start the game and board");
            yield return Session.StartGameCoroutine();
            if (atStation != null)
                yield return atStation();

            _configOverride.AssertBound();
            Flight = new LoopSmokeFlight(Session, new LoopSmokeFieldCheck(ProjectContext.Instance.Container));
            Flight.InstallRequiredEngines();
            yield return Flight.BoardPlayerCoroutine();
            yield return WaitForReportCoroutine(CoopTestSteps.BOARDED);
            yield return WaitForCoroutine(IsClientOnDeckOnServer, "the client on the deck by its server position");
        }

        // onCruise: what the host pilot does at once when the cruise starts (the short test cruise lands soon).
        public IEnumerator WaitForCruiseCoroutine(int loopIndex, Action onCruise) {
            BeginStep($"loop {loopIndex} cruise");
            yield return WaitForCoroutine(() => Model.Phase == ShipRunPhase.Cruise, $"Cruise of loop {loopIndex}");
            onCruise();
            // The ship takes the faced heading in its LateUpdate.
            yield return null;
            yield return WaitForReportCoroutine(CoopTestSteps.IN_CRUISE);
            AssertSameView(true);
        }

        public IEnumerator WaitForLandingCoroutine(int loopIndex) {
            BeginStep($"loop {loopIndex} landing");
            yield return WaitForCoroutine(() => SessionQueries.IsAtStationAfterLoop(loopIndex), $"Build after loop {loopIndex}");
            Assert.AreEqual(ShipRunAbortReason.None, Model.LastAbortReason, "The flight ended as a wreck.");
            yield return WaitForReportCoroutine(CoopTestSteps.LANDED);
            Assert.IsTrue(IsClientOnDeckOnServer(), $"The client is not on the deck by its server position after loop {loopIndex}: {ClientServerPosition()}.");
            AssertSameView(false);
        }

        public IEnumerator FinishCoroutine() {
            BeginStep("client leaves and quits");
            Command(CoopTestSteps.COMMAND_FINISH);
            yield return WaitForReportCoroutine(CoopTestSteps.FINISHED);
            Assert.IsEmpty(LastReport.Errors, $"The client logged errors:\n{string.Join("\n", LastReport.Errors)}");
            float deadline = Time.realtimeSinceStartup + CLIENT_EXIT_TIMEOUT_SECONDS;
            while (_client.HasExited == false && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.IsTrue(_client.HasExited, "The client did not quit after its last step.");
            _client.Dispose();
            _client = null;
            yield return WaitForCoroutine(IsClientGone, "the client's connection gone on the server");

            BeginStep("host leaves to the menu");
            yield return Session.LeaveToMenuCoroutine();
            Debug.Log($"[CoopTest] the whole scenario took {Time.realtimeSinceStartup - _testStarted:0.0} s");
            Assert.AreEqual(0, ErrorLog.Count, $"The host logged errors:\n{ErrorLog.Describe()}");
        }

        // The short test cruise would reach the station while the client works: a cruise heads away from it first and
        // turns to it (LoopSmokeFlight.FaceDestination) once the client is done.
        public void FaceAwayFromStation() =>
            Ship.FlightControlDebug.DebugFace(Ship.transform.position - Model.TransitDestination);

        // The ship takes a faced heading at its turn rate: a half turn takes seconds, and the crew turns with it.
        public IEnumerator WaitForTurnDoneCoroutine() {
            float lastYaw = Ship.transform.eulerAngles.y;
            float steadySince = Time.time;
            while (Time.time - steadySince < STEADY_HEADING_SECONDS) {
                yield return null;
                float yaw = Ship.transform.eulerAngles.y;
                if (Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) > STEADY_YAW_DEGREES_PER_FRAME)
                    steadySince = Time.time;

                lastYaw = yaw;
                if (Time.time - steadySince > STEP_TIMEOUT_SECONDS)
                    Assert.Fail("The ship never stopped turning.");
            }
        }

        public void Command(string command) =>
            _exchange.AppendCommand(command);

        public IEnumerator CommandAndWaitCoroutine(string command, string step) {
            Command(command);
            yield return WaitForReportCoroutine(step);
        }

        // Takes the client's next report and fails at once when the client failed, quit or went silent.
        public IEnumerator WaitForReportCoroutine(string step) {
            LastReport = null;
            yield return WaitForCoroutine(() => TryTakeReport(step), $"the client's '{step}' report", _exchangePoll);
            Debug.Log($"[CoopTest] client: {LastReport.Describe()}\n{LastReport.DescribeFlight()}");
        }

        // Calls everyFrame every frame until the report arrives: the host watches its own side meanwhile.
        public IEnumerator WaitForReportCoroutine(string step, Action everyFrame) {
            LastReport = null;
            _everyFrame = everyFrame;
            yield return WaitForCoroutine(() => WatchAndTakeReport(step), $"the client's '{step}' report");
            _everyFrame = null;
            Debug.Log($"[CoopTest] client: {LastReport.Describe()}\n{LastReport.DescribeFlight()}");
        }

        // pause: what to wait between checks; null checks every frame.
        public IEnumerator WaitForCoroutine(Func<bool> condition, string target, YieldInstruction pause = null) {
            float deadline = Time.realtimeSinceStartup + STEP_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {STEP_TIMEOUT_SECONDS} s waiting for {target}. Host errors:\n{ErrorLog.Describe()}\n{DescribeClient()}");

                if (_client != null && _client.HasExited)
                    Assert.Fail($"The client quit while the host waited for {target}.\n{_client.DescribeLogTail()}");

                yield return pause;
            }
        }

        public void BeginStep(string step) {
            float now = Time.realtimeSinceStartup;
            Debug.Log($"[CoopTest] {step} (at {now - _testStarted:0.0} s; the previous step took {now - _stepStarted:0.0} s)");
            _stepStarted = now;
        }

        // The common fields (CoopObservationComparison) of the client's last report against the host's own view now.
        public void AssertSameView(bool isInFlight) {
            CoopObservation host = CoopObserver.CaptureGame(LastReport.Step);
            List<string> mismatches = CoopObservationComparison.FindMismatches(host, LastReport, isInFlight, true);
            Assert.IsEmpty(mismatches, $"Host and client differ at {LastReport.Step}:\n{string.Join("\n", mismatches)}\n"
                                       + $"host: {host.Describe()}\nclient: {LastReport.Describe()}");
        }

        public ShipRider ClientRider() {
            Assert.IsTrue(TryGetClientConnection(out NetworkConnectionToClient connection) && connection.identity != null, "The client has no player on the server.");
            return connection.identity.GetComponent<ShipRider>();
        }

        public bool IsClientPlayerSpawned() =>
            TryGetClientConnection(out NetworkConnectionToClient connection) && connection.identity != null;

        public bool IsClientOnDeckOnServer() =>
            IsClientPlayerSpawned() && SessionQueries.IsOnDeck(Ship, ClientServerPosition());

        public Vector3 ClientServerPosition() =>
            TryGetClientConnection(out NetworkConnectionToClient connection) && connection.identity != null
                ? connection.identity.transform.position
                : Vector3.positiveInfinity;

        private bool WatchAndTakeReport(string step) {
            _everyFrame();
            return TryTakeReport(step);
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
            LastReport = report;
            return true;
        }

        private string DescribeClient() =>
            _client == null ? "(no client started)" : _client.DescribeLogTail();

        private static bool IsClientGone() =>
            TryGetClientConnection(out NetworkConnectionToClient _) == false;

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

        // Into Builds/CoopScreenshots/<test name>/, outside Assets and git-ignored like the client build.
        private void KeepScreenshots() {
            if (_runDirectory == null || Directory.Exists(_runDirectory) == false)
                return;

            string[] screenshots = Directory.GetFiles(_runDirectory, SCREENSHOT_PATTERN);
            if (screenshots.Length == 0)
                return;

            string target = Path.Combine(CoopClientBuildPaths.ProjectRoot, SCREENSHOTS_FOLDER, TestContext.CurrentContext.Test.Name);
            Directory.CreateDirectory(target);
            foreach (string screenshot in screenshots)
                File.Copy(screenshot, Path.Combine(target, Path.GetFileName(screenshot)), true);
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
