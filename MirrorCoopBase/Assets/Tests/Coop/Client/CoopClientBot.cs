#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using Features.CharacterMovableModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // The common part of a coop client scenario (the coop flight tests): joins the host by direct IP through the menu,
    // boards, waits for the host's commands, reports what it sees into the exchange folder, captures the screen, and
    // leaves and quits at the end. The host test decides pass or fail.
    public abstract class CoopClientBot : MonoBehaviour {
        private const float STEP_TIMEOUT_SECONDS = 120f;
        private const float SETTLE_SECONDS = 1.5f;
        private const int FAILURE_EXIT_CODE = 1;

        private readonly WaitForSeconds _exchangePoll = new WaitForSeconds(CoopTestExchange.POLL_SECONDS);

        private CoopTestArguments _arguments;
        private CoopTestExchange _exchange;
        private LoopSmokeErrorLog _errorLog;
        private Coroutine _scenario;
        private bool _hasFailed;

        protected readonly WaitForSeconds Settle = new WaitForSeconds(SETTLE_SECONDS);

        protected static ShipBase Ship => Object.FindAnyObjectByType<ShipBase>();

        protected static IReadOnlyShipRunModel RunModel => SessionQueries.Resolve<IReadOnlyShipRunModel>();

        public void Run(CoopTestArguments arguments) {
            _arguments = arguments;
            _exchange = new CoopTestExchange(arguments.ExchangeDirectory);
            _errorLog = new LoopSmokeErrorLog();
            // The client window is not focused while the editor hosts the test.
            Application.runInBackground = true;
            _scenario = StartCoroutine(RunWholeScenarioCoroutine());
        }

        private void OnDestroy() {
            if (_scenario != null)
                StopCoroutine(_scenario);

            _scenario = null;
            _errorLog.Dispose();
        }

        // Between joining the lobby and leaving at the host's finish command.
        protected abstract IEnumerator RunScenarioCoroutine();

        private IEnumerator RunWholeScenarioCoroutine() {
            yield return WaitForCoroutine(SessionQueries.IsMenuReady, "the menu");
            CoopSessionActions.PrepareDirect(_arguments.Port, CoopTestPlayers.CLIENT_NAME);
            CoopSessionActions.JoinThroughMenu(_arguments.Address);
            yield return WaitForCoroutine(CoopSessionActions.IsClientInLobby, "the host's lobby");
            Report(CoopObserver.CaptureStep(CoopTestSteps.IN_LOBBY));
            yield return RunScenarioCoroutine();
            yield return FinishCoroutine();
        }

        // At the stern: the client must ride away from wherever on the deck it stands, also farthest from the pivot.
        protected IEnumerator BoardCoroutine() {
            yield return WaitForCoroutine(SessionQueries.IsGameAtStation, "GameScene at the start station");
            ShipDeckBoarding.PutAboveStern(SessionQueries.LocalPlayer.GetComponent<CharacterMovableBase>(), Ship);
            yield return Settle;
            Report(CoopFlightObserver.CaptureFlight(CoopTestSteps.BOARDED));
        }

        protected IEnumerator WaitForCruiseCoroutine(int loopIndex) {
            yield return WaitForCoroutine(() => IsInCruiseOfLoop(loopIndex), $"Cruise of loop {loopIndex}");
            Report(CoopFlightObserver.CaptureFlight(CoopTestSteps.IN_CRUISE));
        }

        protected IEnumerator WaitForLandingCoroutine(int loopIndex) {
            yield return WaitForCoroutine(() => SessionQueries.IsAtStationAfterLoop(loopIndex), $"Build after loop {loopIndex}");
            yield return Settle;
            Report(CoopFlightObserver.CaptureFlight(CoopTestSteps.LANDED));
        }

        protected IEnumerator WaitForCommandCoroutine(string command) =>
            WaitForCoroutine(() => _exchange.HasCommand(command), $"the host's {command} command", _exchangePoll);

        // Fails the scenario with the step's message when a shared helper reports a failure.
        protected IEnumerator FailIfCoroutine(string failure, string step) {
            if (failure != null)
                yield return FailCoroutine($"{step}: {failure}.");
        }

        // pause: what to wait between checks; null checks every frame.
        protected IEnumerator WaitForCoroutine(Func<bool> condition, string target, YieldInstruction pause = null) {
            float deadline = Time.realtimeSinceStartup + STEP_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    yield return FailCoroutine($"Timed out after {STEP_TIMEOUT_SECONDS} s waiting for {target}.");

                yield return pause;
            }
        }

        // Reports the failure and quits; never returns, so no later step reports anything.
        protected IEnumerator FailCoroutine(string message) {
            CoopObservation observation = CoopObserver.CaptureStep(CoopTestSteps.FAILED);
            observation.Message = message;
            observation.Errors = CollectErrors();
            Report(observation);
            _hasFailed = true;
            Application.Quit(FAILURE_EXIT_CODE);
            while (true)
                yield return null;
        }

        protected void Report(CoopObservation observation) {
            if (_hasFailed)
                return;

            _exchange.AppendReport(observation);
            Debug.Log($"[CoopClient] {observation.Describe()} | {observation.DescribeFlight()}");
        }

        // Into the exchange folder at the end of this frame; the host keeps the client's screenshots of a run.
        protected IEnumerator CaptureScreenCoroutine(string name, Action<string> captured) {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(_arguments.ExchangeDirectory, CoopFlightSteps.CLIENT_SCREENSHOT_PREFIX + name + CoopFlightSteps.SCREENSHOT_EXTENSION);
            ScreenCapture.CaptureScreenshot(path);
            captured(path);
        }

        private IEnumerator FinishCoroutine() {
            yield return WaitForCommandCoroutine(CoopTestSteps.COMMAND_FINISH);
            CoopSessionActions.LeaveThroughHud();
            yield return WaitForCoroutine(SessionQueries.IsBackInMenu, "the menu after the last step");
            CoopObservation observation = CoopObserver.CaptureStep(CoopTestSteps.FINISHED);
            observation.Errors = CollectErrors();
            Report(observation);
            Application.Quit();
        }

        // In flight the loop index still counts the flights finished before this one.
        private static bool IsInCruiseOfLoop(int loopIndex) =>
            SessionQueries.LocalPlayer != null && RunModel.Phase == ShipRunPhase.Cruise && RunModel.LoopIndex == loopIndex - 1;

        private string[] CollectErrors() =>
            _errorLog.Count == 0 ? Array.Empty<string>() : new[] { _errorLog.Describe() };
    }
}
#endif
