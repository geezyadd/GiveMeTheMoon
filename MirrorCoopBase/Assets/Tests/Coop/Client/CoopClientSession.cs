#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using Features.GameFlowStateMachineModule.Scripts;
using Features.ShipModule.Scripts;
using Mirror;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // What every step of a coop test client needs: waits that fail the client on a timeout, the failure report that
    // quits the process, and the report of what this process sees now, with its logged errors.
    public sealed class CoopClientSession : IDisposable {
        // Below the host's step timeout, so the host gets this client's failure report before it gives up itself.
        private const float STEP_TIMEOUT_SECONDS = 80f;
        private const int FAILURE_EXIT_CODE = 1;
        private const string FAILURE_SCREENSHOT = "failure";
        private const int SCREENSHOT_FRAMES = 3;

        private readonly LoopSmokeErrorLog _errorLog = new LoopSmokeErrorLog();
        private bool _hasFailed;

        public CoopTestArguments Arguments { get; }
        public CoopTestExchange Exchange { get; }

        public CoopClientSession(CoopTestArguments arguments) {
            Arguments = arguments;
            Exchange = new CoopTestExchange(arguments.ExchangeDirectory);
        }

        public void Dispose() =>
            _errorLog.Dispose();

        // The game's view while a ship is loaded and this client has a player, else only the step; always the run state
        // and the errors this client logged so far.
        public CoopObservation Capture(string step) {
            bool isInGame = SessionQueries.LocalPlayer != null && Object.FindAnyObjectByType<ShipBase>() != null;
            CoopObservation observation = isInGame ? CoopObserver.CaptureGame(step) : CoopObserver.CaptureStep(step);
            observation.RunState = CoopRunStateObserver.Capture();
            observation.Errors = CollectErrors();
            return observation;
        }

        public void Report(CoopObservation observation) {
            if (_hasFailed)
                return;

            Exchange.AppendReport(observation);
            Debug.Log($"[CoopClient] {observation.Describe()}; {observation.RunState?.Describe()}");
        }

        // pause: what to wait between checks; null checks every frame.
        public IEnumerator WaitForCoroutine(Func<bool> condition, string target, YieldInstruction pause = null) {
            float deadline = Time.realtimeSinceStartup + STEP_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    yield return FailCoroutine($"Timed out after {STEP_TIMEOUT_SECONDS} s waiting for {target}. {DescribeSession()}");

                yield return pause;
            }
        }

        // Reports the failure with a screenshot and quits; never returns, so no later step reports anything.
        public IEnumerator FailCoroutine(string message) {
            CoopObservation observation = CoopObserver.CaptureStep(CoopTestSteps.FAILED);
            observation.Message = message;
            observation.Errors = CollectErrors();
            observation.RunState = CoopRunStateObserver.Capture();
            Report(observation);
            _hasFailed = true;
            ScreenCapture.CaptureScreenshot(ScreenshotPath(FAILURE_SCREENSHOT));
            for (int frame = 0; frame < SCREENSHOT_FRAMES; frame++)
                yield return null;

            Application.Quit(FAILURE_EXIT_CODE);
            while (true)
                yield return null;
        }

        public string ScreenshotPath(string name) =>
            Path.Combine(Arguments.ExchangeDirectory, CoopConnectionCommands.SCREENSHOT_PREFIX + name + CoopConnectionCommands.SCREENSHOT_EXTENSION);

        // Where this process stands when a wait times out: the network, the scenes and the game flow.
        private static string DescribeSession() {
            IGameFlowStateMachineService flow = SessionQueries.Resolve<IGameFlowStateMachineService>();
            return $"Session: client active {NetworkClient.active}, connected {NetworkClient.isConnected}, server {NetworkServer.active}, "
                   + $"active scene {SceneManager.GetActiveScene().name}, scenes {SceneManager.sceneCount}, menu ready {SessionQueries.IsMenuReady()}, "
                   + $"flow {flow.CurrentStateType?.Name} (transitioning {flow.IsTransitioning}).";
        }

        private string[] CollectErrors() =>
            _errorLog.Count == 0 ? Array.Empty<string>() : new[] { _errorLog.Describe() };
    }
}
#endif
