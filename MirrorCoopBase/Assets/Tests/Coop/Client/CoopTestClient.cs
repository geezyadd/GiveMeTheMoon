#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using Features.CharacterMovableModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.PlayerLifeModule.Scripts.Spectator;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.SonarModule.Data.Generated;
using Game.Connection;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // The second player of the coop test, in a development build: joins the host by direct IP through the menu,
    // boards, rides the loops the host flies, leaves in flight, is turned away in flight, comes back at the next
    // station, dies there and waits to be bought back, and reports what it sees at every step into the exchange
    // folder. The host test decides pass or fail.
    public sealed class CoopTestClient : MonoBehaviour {
        private const float STEP_TIMEOUT_SECONDS = 120f;
        private const float SETTLE_SECONDS = 1.5f;
        private const int LEAVE_LOOP = 2;
        private const int LAST_LOOP = 3;
        private const int FAILURE_EXIT_CODE = 1;

        private readonly WaitForSeconds _settle = new WaitForSeconds(SETTLE_SECONDS);
        private readonly WaitForSeconds _exchangePoll = new WaitForSeconds(CoopTestExchange.POLL_SECONDS);

        private CoopTestArguments _arguments;
        private CoopTestExchange _exchange;
        private LoopSmokeErrorLog _errorLog;
        private Coroutine _scenario;
        private bool _hasFailed;

        public void Run(CoopTestArguments arguments) {
            _arguments = arguments;
            _exchange = new CoopTestExchange(arguments.ExchangeDirectory);
            _errorLog = new LoopSmokeErrorLog();
            // The client window is not focused while the editor hosts the test.
            Application.runInBackground = true;
            _scenario = StartCoroutine(RunScenarioCoroutine());
        }

        private void OnDestroy() {
            if (_scenario != null)
                StopCoroutine(_scenario);

            _scenario = null;
            _errorLog.Dispose();
        }

        private IEnumerator RunScenarioCoroutine() {
            if (_arguments.Scenario != CoopTestSteps.SCENARIO_LEAVE_AND_REJOIN)
                yield return FailCoroutine($"Unknown scenario {_arguments.Scenario}.");

            yield return JoinLobbyCoroutine();
            yield return WaitForCoroutine(SessionQueries.IsGameAtStation, "GameScene at the start station");
            yield return BoardCoroutine();
            yield return RideLoopCoroutine(1);
            yield return LeaveInFlightCoroutine();
            yield return KnockInFlightCoroutine();
            yield return RejoinAtStationCoroutine();
            yield return DieCoroutine();
            yield return WaitForBuyBackCoroutine();
            yield return RideLoopCoroutine(LAST_LOOP);
            yield return FinishCoroutine();
        }

        private IEnumerator JoinLobbyCoroutine() {
            yield return WaitForCoroutine(SessionQueries.IsMenuReady, "the menu");
            JoinThroughMenu();
            yield return WaitForCoroutine(CoopSessionActions.IsClientInLobby, "the host's lobby");
            Report(CoopObserver.CaptureStep(CoopTestSteps.IN_LOBBY));
        }

        // At the stern: the client must ride away from wherever on the deck it stands, also farthest from the pivot.
        private IEnumerator BoardCoroutine() {
            ShipBase ship = Object.FindAnyObjectByType<ShipBase>();
            ShipDeckBoarding.PutAboveStern(SessionQueries.LocalPlayer.GetComponent<CharacterMovableBase>(), ship);
            yield return _settle;
            Report(CoopObserver.CaptureGame(CoopTestSteps.BOARDED));
        }

        private IEnumerator RideLoopCoroutine(int loopIndex) {
            yield return WaitForCruiseCoroutine(loopIndex);
            if (loopIndex == CoopTestSteps.SONAR_MARK_LOOP)
                yield return WaitForSonarMarkCoroutine();

            yield return WaitForCoroutine(() => SessionQueries.IsAtStationAfterLoop(loopIndex), $"Build after loop {loopIndex}");
            yield return _settle;
            Report(CoopObserver.CaptureGame(CoopTestSteps.LANDED));
        }

        // The host navigator marks a contact in this cruise: the mark must reach this client's synced sonar model.
        private IEnumerator WaitForSonarMarkCoroutine() {
            yield return WaitForCoroutine(() => SessionQueries.Resolve<IReadOnlySonarConsoleModel>().Marks.Count > 0, "the host navigator's sonar mark");
            Report(CoopObserver.CaptureGame(CoopTestSteps.SONAR_MARK_SEEN));
        }

        private IEnumerator LeaveInFlightCoroutine() {
            yield return WaitForCruiseCoroutine(LEAVE_LOOP);
            CoopSessionActions.LeaveThroughHud();
            yield return WaitForCoroutine(SessionQueries.IsBackInMenu, "the menu after leaving in flight");
            Report(CoopObserver.CaptureStep(CoopTestSteps.LEFT_IN_FLIGHT));
        }

        private IEnumerator KnockInFlightCoroutine() {
            JoinThroughMenu();
            yield return WaitForCoroutine(CoopSessionActions.IsRejectedInMenu, "the answer to a join in flight");
            ConnectionRejectReason reason = SessionQueries.Resolve<ConnectionRejectModel>().LastRejection;
            CoopObservation observation = CoopObserver.CaptureStep(CoopTestSteps.REJECTED_IN_FLIGHT);
            observation.RejectReason = reason.ToString();
            observation.IsRejectTextShown = CoopSessionActions.IsRejectTextShown(reason);
            Report(observation);
        }

        private IEnumerator RejoinAtStationCoroutine() {
            yield return WaitForCoroutine(() => _exchange.HasCommand(CoopTestSteps.COMMAND_REJOIN), "the host's rejoin command", _exchangePoll);
            JoinThroughMenu();
            yield return WaitForCoroutine(SessionQueries.IsGameAtStation, "GameScene after rejoining at a station");
            yield return _settle;
            Report(CoopObserver.CaptureGame(CoopTestSteps.REJOINED));
        }

        // The development-build kill, the same command as F9: the death itself is the server's.
        private IEnumerator DieCoroutine() {
            SessionQueries.LocalPlayer.GetComponent<PlayerLifeBody>().CmdDebugKill();
            yield return WaitForCoroutine(IsDeadAndSpectating, "the local player dead and spectating");
            Report(CoopObserver.CaptureGame(CoopTestSteps.DIED));
        }

        private IEnumerator WaitForBuyBackCoroutine() {
            yield return WaitForCoroutine(() => LocalLifeState() == PlayerLifeState.Alive, "being bought back by the host");
            yield return _settle;
            Report(CoopObserver.CaptureGame(CoopTestSteps.REVIVED));
        }

        private IEnumerator FinishCoroutine() {
            yield return WaitForCoroutine(() => _exchange.HasCommand(CoopTestSteps.COMMAND_FINISH), "the host's finish command", _exchangePoll);
            CoopSessionActions.LeaveThroughHud();
            yield return WaitForCoroutine(SessionQueries.IsBackInMenu, "the menu after the last loop");
            CoopObservation observation = CoopObserver.CaptureStep(CoopTestSteps.FINISHED);
            observation.Errors = CollectErrors();
            Report(observation);
            Application.Quit();
        }

        private IEnumerator WaitForCruiseCoroutine(int loopIndex) {
            yield return WaitForCoroutine(() => IsInCruiseOfLoop(loopIndex), $"Cruise of loop {loopIndex}");
            Report(CoopObserver.CaptureGame(CoopTestSteps.IN_CRUISE));
        }

        private void JoinThroughMenu() {
            CoopSessionActions.PrepareDirect(_arguments.Port, CoopTestPlayers.CLIENT_NAME);
            CoopSessionActions.JoinThroughMenu(_arguments.Address);
        }

        // In flight the loop index still counts the flights finished before this one.
        private static bool IsInCruiseOfLoop(int loopIndex) =>
            SessionQueries.LocalPlayer != null && RunModel().Phase == ShipRunPhase.Cruise && RunModel().LoopIndex == loopIndex - 1;

        private static bool IsDeadAndSpectating() =>
            LocalLifeState() == PlayerLifeState.Dead && SessionQueries.Resolve<SpectatorModel>().IsSpectating;

        private static PlayerLifeState LocalLifeState() =>
            SessionQueries.Resolve<IPlayerLifeQuery>().LocalState;

        private static IReadOnlyShipRunModel RunModel() =>
            SessionQueries.Resolve<IReadOnlyShipRunModel>();

        // pause: what to wait between checks; null checks every frame.
        private IEnumerator WaitForCoroutine(Func<bool> condition, string target, YieldInstruction pause = null) {
            float deadline = Time.realtimeSinceStartup + STEP_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    yield return FailCoroutine($"Timed out after {STEP_TIMEOUT_SECONDS} s waiting for {target}.");

                yield return pause;
            }
        }

        // Reports the failure and quits; never returns, so no later step reports anything.
        private IEnumerator FailCoroutine(string message) {
            CoopObservation observation = CoopObserver.CaptureStep(CoopTestSteps.FAILED);
            observation.Message = message;
            observation.Errors = CollectErrors();
            Report(observation);
            _hasFailed = true;
            Application.Quit(FAILURE_EXIT_CODE);
            while (true)
                yield return null;
        }

        private string[] CollectErrors() =>
            _errorLog.Count == 0 ? Array.Empty<string>() : new[] { _errorLog.Describe() };

        private void Report(CoopObservation observation) {
            if (_hasFailed)
                return;

            _exchange.AppendReport(observation);
            Debug.Log($"[CoopClient] {observation.Describe()}");
        }
    }
}
#endif
