#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Features.ShipModule.Scripts;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Client;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using Zenject;

namespace Tests.Coop.Host {
    // What the economy and weapons coop tests share (CoopEconomyTest, CoopWeaponsTest): the host with the loop test's
    // fast flight timings, the client build started with a scenario, the host's commands in order and the client's
    // reports one by one, waits that fail at once when the client failed, quit or went silent, the client's player on
    // the server, the same-view comparison and the finish. A passed run deletes its exchange folder but keeps the
    // client's screenshots in Temp/CoopClient.
    public sealed class CoopEconomyHostHarness : IDisposable {
        private const float STEP_TIMEOUT_SECONDS = 90f;
        private const float CLIENT_EXIT_TIMEOUT_SECONDS = 30f;
        private const string LOOPBACK_ADDRESS = "127.0.0.1";
        private const string CLIENT_LOG_FILE = "client.log";
        private const string RUN_FOLDER_FORMAT = "yyyyMMdd-HHmmss";
        private const string SCREENSHOT_FOLDER = "Temp/CoopClient";
        private const string SCREENSHOT_PATTERN = CoopClientScreenshots.FILE_PREFIX + "*.png";

        private readonly LoopSmokeShipRunConfigOverride _configOverride;
        private readonly WaitForSeconds _exchangePoll = new WaitForSeconds(CoopTestExchange.POLL_SECONDS);
        private string _runDirectory;
        private CoopTestExchange _exchange;
        private CoopClientProcess _client;
        private int _readReports;
        private float _testStarted;
        private float _stepStarted;

        public LoopSmokeErrorLog ErrorLog { get; }
        public LoopSmokeSession Session { get; }
        public LoopSmokeFlight Flight { get; private set; }
        public CoopObservation LastReport { get; private set; }

        public ShipBase Ship =>
            Session.Ship;

        public CoopEconomyHostHarness() {
            _configOverride = new LoopSmokeShipRunConfigOverride(LoopSmokeShipRunConfigOverride.FAST_TIMINGS_JSON);
            ErrorLog = new LoopSmokeErrorLog();
            Session = new LoopSmokeSession(ErrorLog);
        }

        public void Dispose() {
            _client?.Dispose();
            ErrorLog.Dispose();
            KeepScreenshots();
            DeletePassedRunDirectory();
            _configOverride.Dispose();
        }

        public IEnumerator HostAndStartWithClientCoroutine(string scenario) {
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

            BeginStep("start the game");
            yield return Session.StartGameCoroutine();
            _configOverride.AssertBound();
            Flight = new LoopSmokeFlight(Session, new LoopSmokeFieldCheck(ProjectContext.Instance.Container));
            yield return Flight.BoardPlayerCoroutine();
            yield return WaitForReportCoroutine(CoopWeaponsEconomySteps.BOARDED);
        }

        public void SendCommand(string command, params object[] arguments) {
            List<string> parts = new List<string> { command };
            foreach (object argument in arguments)
                parts.Add(Convert.ToString(argument, System.Globalization.CultureInfo.InvariantCulture));

            string line = string.Join(CoopWeaponsEconomySteps.ARGUMENT_SEPARATOR.ToString(), parts);
            Debug.Log($"[CoopTest] command to the client: {line}");
            _exchange.AppendCommand(line);
        }

        // Sends a command and takes the client's report of it.
        public IEnumerator CommandCoroutine(string reportedStep, string command, params object[] arguments) {
            SendCommand(command, arguments);
            yield return WaitForReportCoroutine(reportedStep);
        }

        public IEnumerator FinishCoroutine() {
            BeginStep("client leaves and quits");
            SendCommand(CoopWeaponsEconomySteps.COMMAND_FINISH);
            yield return WaitForReportCoroutine(CoopTestSteps.FINISHED);
            Assert.IsEmpty(LastReport.Errors, $"The client logged errors:\n{string.Join("\n", LastReport.Errors)}");
            float deadline = Time.realtimeSinceStartup + CLIENT_EXIT_TIMEOUT_SECONDS;
            while (_client.HasExited == false && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.IsTrue(_client.HasExited, "The client did not quit after its last step.");
            yield return WaitForCoroutine(IsClientGone, "the client's connection gone on the server");

            BeginStep("host leaves to the menu");
            yield return Session.LeaveToMenuCoroutine();
            Debug.Log($"[CoopTest] the whole scenario took {Time.realtimeSinceStartup - _testStarted:0.0} s");
            Assert.AreEqual(0, ErrorLog.Count, $"The host logged errors:\n{ErrorLog.Describe()}");
        }

        // The base comparison (run, ship, stats, balance, nameplates, the client on the deck) where the client stands on
        // the deck, and always the economy and weapons fields.
        public void AssertSameView(bool isInFlight, bool isClientOnDeck) {
            CoopObservation host = CoopWeaponsEconomyObserver.Capture(LastReport.Step);
            List<string> mismatches = isClientOnDeck
                ? CoopObservationComparison.FindMismatches(host, LastReport, isInFlight, true)
                : new List<string>();
            mismatches.AddRange(CoopWeaponsEconomyComparison.FindMismatches(host, LastReport));
            Assert.IsEmpty(mismatches, $"Host and client differ at {LastReport.Step}:\n{string.Join("\n", mismatches)}\n"
                                       + $"host: {host.Describe()}\nclient: {LastReport.Describe()}");
        }

        public void BeginStep(string step) {
            float now = Time.realtimeSinceStartup;
            string ship = Session.Ship != null ? $", ship at {Session.Ship.transform.position} turned {Session.Ship.transform.eulerAngles}, deck top {Session.Ship.DeckGeometry.Bounds.max.y:0.00}" : string.Empty;
            Debug.Log($"[CoopTest] {step} (at {now - _testStarted:0.0} s; the previous step took {now - _stepStarted:0.0} s{ship})");
            _stepStarted = now;
        }

        public IEnumerator WaitForReportCoroutine(string step) =>
            WaitForReportCoroutine(step, step);

        // The client's next report, which is one of two steps.
        public IEnumerator WaitForReportCoroutine(string step, string otherStep) {
            LastReport = null;
            yield return WaitForCoroutine(() => TryTakeReport(step, otherStep), $"the client's '{step}' report", _exchangePoll);
            Debug.Log($"[CoopTest] client: {LastReport.Describe()}");
        }

        public bool TryTakeReport(string step) =>
            TryTakeReport(step, step);

        // Takes the client's next report, if it has come; fails at once when it is a failure or another step.
        public bool TryTakeReport(string step, string otherStep) {
            List<CoopObservation> reports = _exchange.ReadReportsFrom(_readReports);
            if (reports.Count == 0)
                return false;

            CoopObservation report = reports[0];
            _readReports++;
            if (report.Step == CoopTestSteps.FAILED)
                Assert.Fail($"The client failed: {report.Message}\n{string.Join("\n", report.Errors)}\n{_client.DescribeLogTail()}");

            Assert.IsTrue(report.Step == step || report.Step == otherStep, $"The client reported another step than '{step}': {report.Describe()}");
            LastReport = report;
            return true;
        }

        // pause: what to wait between checks; null checks every frame.
        public IEnumerator WaitForCoroutine(Func<bool> condition, string target, YieldInstruction pause = null) {
            float deadline = Time.realtimeSinceStartup + STEP_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {STEP_TIMEOUT_SECONDS} s waiting for {target}. Host errors:\n{ErrorLog.Describe()}\n{_client?.DescribeLogTail()}");

                if (_client != null && _client.HasExited)
                    Assert.Fail($"The client quit while the host waited for {target}.\n{_client.DescribeLogTail()}");

                yield return pause;
            }
        }

        public static NetworkIdentity ClientPlayer() {
            Assert.IsTrue(TryGetClientConnection(out NetworkConnectionToClient connection) && connection.identity != null, "The client has no player on the server.");
            return connection.identity;
        }

        public static ShipRider ClientRider() =>
            ClientPlayer().GetComponent<ShipRider>();

        public static NetworkConnectionToClient ClientConnection() {
            Assert.IsTrue(TryGetClientConnection(out NetworkConnectionToClient connection), "The client is not connected.");
            return connection;
        }

        public bool IsClientOnDeckOnServer() =>
            IsClientPlayerSpawned() && SessionQueries.IsOnDeck(Ship, ClientPlayer().transform.position);

        private static bool IsClientPlayerSpawned() =>
            TryGetClientConnection(out NetworkConnectionToClient connection) && connection.identity != null;

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

        private void KeepScreenshots() {
            if (_runDirectory == null || Directory.Exists(_runDirectory) == false)
                return;

            string folder = Path.Combine(CoopClientBuildPaths.ProjectRoot, SCREENSHOT_FOLDER);
            Directory.CreateDirectory(folder);
            foreach (string screenshot in Directory.GetFiles(_runDirectory, SCREENSHOT_PATTERN))
                File.Copy(screenshot, Path.Combine(folder, Path.GetFileName(screenshot)), true);
        }

        // A passed run leaves nothing behind; a failed one keeps the client's log and the exchange files for a look.
        private void DeletePassedRunDirectory() {
            bool isPassed = TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Passed;
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
