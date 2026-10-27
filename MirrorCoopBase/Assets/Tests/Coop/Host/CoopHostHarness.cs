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
using UnityEngine.TestTools;
using Zenject;

namespace Tests.Coop.Host {
    // The editor's side of the connection tests: hosts on a free port, starts client processes (CoopClientBot) and
    // sends them commands, plays the run (start, launch, land) and waits with a timeout that names what it waited for.
    // Dispose kills every client, keeps the client screenshots and deletes a passed run's folder.
    public sealed class CoopHostHarness : IDisposable {
        public const string CLIENT_NAME = CoopTestPlayers.CLIENT_NAME;
        public const string SECOND_CLIENT_NAME = "CoopClient2";
        private const float STEP_TIMEOUT_SECONDS = 90f;
        private const float CLIENT_EXIT_TIMEOUT_SECONDS = 30f;
        private const string LOOPBACK_ADDRESS = "127.0.0.1";
        private const string RUN_FOLDER_FORMAT = "yyyyMMdd-HHmmss";
        private const string SCREENSHOTS_FOLDER = "Builds/CoopScreenshots";
        private const string SCREENSHOT_PATTERN = CoopConnectionCommands.SCREENSHOT_PREFIX + "*" + CoopConnectionCommands.SCREENSHOT_EXTENSION;

        // The Test Runner plays all tests of a run in one Play mode session with one ProjectContext: the first test binds
        // the fast timings for all of them; the domain reload of the next Play mode drops it.
        private static LoopSmokeShipRunConfigOverride _configOverride;

        private readonly LoopSmokeErrorLog _errorLog = new LoopSmokeErrorLog();
        private readonly List<CoopClientBot> _bots = new List<CoopClientBot>();
        private readonly WaitForSeconds _exchangePoll = new WaitForSeconds(CoopTestExchange.POLL_SECONDS);
        private readonly string _testName;
        private readonly float _testStarted = Time.realtimeSinceStartup;
        private float _stepStarted;
        private ushort _port;
        private string _runDirectory;

        public LoopSmokeSession Session { get; }
        public LoopSmokeFlight Flight { get; private set; }

        public ShipBase Ship =>
            Session.Ship;

        public IReadOnlyShipRunModel Model =>
            Session.Model;

        public CoopHostHarness(string testName) {
            _testName = testName;
            Session = new LoopSmokeSession(_errorLog);
            _configOverride ??= new LoopSmokeShipRunConfigOverride(LoopSmokeShipRunConfigOverride.FAST_TIMINGS_JSON);
        }

        // Kills the clients and leaves a hosted session, so the next test of the run starts from the menu.
        public IEnumerator TearDownCoroutine() {
            KillClients();
            try {
                if (NetworkServer.active)
                    yield return Session.LeaveToMenuCoroutine();
            } finally {
                Dispose();
            }
        }

        public void Dispose() {
            KillClients();
            _errorLog.Dispose();
            KeepScreenshots();
            DeletePassedRunDirectory();
        }

        private void KillClients() {
            foreach (CoopClientBot bot in _bots)
                bot.Dispose();
        }

        // The first step of every test: the Test Runner resets the log assert after SetUp.
        public IEnumerator HostLobbyCoroutine() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            BeginStep("host the lobby");
            _port = FindFreePort();
            _runDirectory = Path.Combine(CoopClientBuildPaths.RunsDirectory, DateTime.Now.ToString(RUN_FOLDER_FORMAT));
            Directory.CreateDirectory(_runDirectory);
            yield return Session.EnterLobbyAsHostCoroutine(() => CoopSessionActions.PrepareDirect(_port, CoopTestPlayers.HOST_NAME));
        }

        // A new process for the player name; a name already started before (a crashed client) gets a new folder.
        public CoopClientBot StartClient(string name) {
            string folder = $"{name}-{_bots.Count + 1}";
            CoopClientBot bot = CoopClientBot.Start(_runDirectory, folder, name, LOOPBACK_ADDRESS, _port);
            _bots.Add(bot);
            return bot;
        }

        public IEnumerator StartClientAndJoinCoroutine(string name, Action<CoopClientBot> started) {
            CoopClientBot bot = StartClient(name);
            started(bot);
            yield return CommandCoroutine(bot, CoopConnectionCommands.JOIN, name);
            yield return WaitForCoroutine(() => bot.TryGetPlayer(out NetworkIdentity _), $"{name}'s player on the server");
        }

        public IEnumerator CommandCoroutine(CoopClientBot bot, string command, string argument = null) {
            BeginStep($"{bot.Name}: {command} {argument}");
            bot.Send(command, argument);
            yield return WaitForCoroutine(() => bot.TryTakeReport(command), $"{bot.Name}'s '{command}' report", _exchangePoll, bot);
            Debug.Log($"[CoopTest] {bot.Name}: {bot.LastReport.Describe()}; {bot.LastReport.RunState?.Describe()}");
        }

        public IEnumerator QuitClientCoroutine(CoopClientBot bot) {
            yield return CommandCoroutine(bot, CoopConnectionCommands.QUIT);
            yield return WaitForCoroutine(() => bot.HasExited, $"{bot.Name}'s process to quit", null, null, CLIENT_EXIT_TIMEOUT_SECONDS);
        }

        // Start, the test's two engines, the host on the deck.
        public IEnumerator StartRunCoroutine() {
            BeginStep("start the run");
            yield return Session.StartGameCoroutine();
            _configOverride.AssertBound();
            Flight = new LoopSmokeFlight(Session, new LoopSmokeFieldCheck(ProjectContext.Instance.Container));
            Flight.InstallRequiredEngines();
            yield return Flight.BoardPlayerCoroutine();
        }

        public IEnumerator LaunchCoroutine(int loopIndex) {
            BeginStep($"loop {loopIndex} takeoff");
            Assert.AreEqual(loopIndex - 1, Model.LoopIndex, "LoopIndex before launch.");
            Flight.PullLaunchLever(loopIndex);
            yield return WaitForCoroutine(() => Model.Phase == ShipRunPhase.Cruise, $"Cruise of loop {loopIndex}");
            Flight.FaceDestination();
        }

        // The short test cruise would reach the station while a client is away: a step that needs the flight to go on
        // heads away from the station first and faces it again when done (Flight.FaceDestination).
        public void FaceAwayFromStation() =>
            Ship.FlightControlDebug.DebugFace(Ship.transform.position - Model.TransitDestination);

        public IEnumerator LandCoroutine(int loopIndex) {
            BeginStep($"loop {loopIndex} landing");
            yield return WaitForCoroutine(() => SessionQueries.IsAtStationAfterLoop(loopIndex), $"Build after loop {loopIndex}");
            Assert.AreEqual(ShipRunAbortReason.None, Model.LastAbortReason, $"Loop {loopIndex} ended as a wreck.");
        }

        // pause: what to wait between checks; null checks every frame. A client that quits fails the wait at once.
        public IEnumerator WaitForCoroutine(Func<bool> condition, string target, YieldInstruction pause = null, CoopClientBot watched = null, float timeoutSeconds = STEP_TIMEOUT_SECONDS) {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {timeoutSeconds} s waiting for {target}. Host errors:\n{_errorLog.Describe()}\n{watched?.DescribeLogTail()}");

                if (watched != null && watched.HasExited)
                    Assert.Fail($"{watched.Name} quit while the host waited for {target}.\n{watched.DescribeLogTail()}");

                yield return pause;
            }
        }

        // The client's last report against the host's own view now; at a station also the hull, its damage and the deck
        // items (in flight the leak moves the hull between the two captures).
        public void AssertSameView(CoopClientBot bot, bool isInFlight) {
            CoopObservation client = bot.LastReport;
            CoopObservation host = CoopObserver.CaptureGame(client.Step);
            host.RunState = CoopRunStateObserver.Capture();
            List<string> mismatches = CoopObservationComparison.FindMismatches(host, client, isInFlight, true);
            if (isInFlight == false)
                mismatches.AddRange(CoopRunStateComparison.FindMismatches(host, client));

            Assert.IsEmpty(mismatches, $"Host and {bot.Name} differ at {client.Step}:\n{string.Join("\n", mismatches)}\n"
                                       + $"host: {host.Describe()}; {host.RunState.Describe()}\n{bot.Name}: {client.Describe()}; {client.RunState.Describe()}");
        }

        public void AssertNoErrors(params CoopClientBot[] bots) {
            foreach (CoopClientBot bot in bots)
                Assert.IsEmpty(bot.LastReport.Errors, $"{bot.Name} logged errors:\n{string.Join("\n", bot.LastReport.Errors)}");

            Assert.AreEqual(0, _errorLog.Count, $"The host logged errors:\n{_errorLog.Describe()}");
        }

        public void BeginStep(string step) {
            float now = Time.realtimeSinceStartup;
            Debug.Log($"[CoopTest] {_testName}: {step} (at {now - _testStarted:0.0} s; the previous step took {now - _stepStarted:0.0} s)");
            _stepStarted = now;
        }

        // The client screenshots go to Builds/CoopScreenshots/<test>/, outside the run folder a passed run deletes.
        private void KeepScreenshots() {
            if (_runDirectory == null || Directory.Exists(_runDirectory) == false)
                return;

            string target = Path.Combine(CoopClientBuildPaths.ProjectRoot, SCREENSHOTS_FOLDER, _testName);
            foreach (string screenshot in Directory.GetFiles(_runDirectory, SCREENSHOT_PATTERN, SearchOption.AllDirectories)) {
                Directory.CreateDirectory(target);
                File.Copy(screenshot, Path.Combine(target, Path.GetFileName(screenshot)), true);
            }
        }

        // A passed run leaves nothing behind; a failed one keeps the clients' logs and the exchange files for a look.
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
