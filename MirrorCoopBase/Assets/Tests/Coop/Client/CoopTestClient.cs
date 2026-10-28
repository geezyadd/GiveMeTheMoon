#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.WeaponModule.Scripts.Readiness;
using Features.PlayerLifeModule.Scripts;
using Features.PlayerLifeModule.Scripts.Spectator;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Generated;
using Features.SonarModule.Data.Generated;
using Game.Connection;
using Mirror;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // The second player of the coop test, in a development build: joins the host by direct IP through the menu,
    // boards, rides the loops the host flies, takes the helm at a station and steers the next flight, leaves in flight
    // from the helm, is turned away in flight, comes back at the next station, dies there and waits to be bought back,
    // launches the last loop by pushing the throttle handle, and reports what it sees at every step into the exchange
    // folder. The host test decides pass or fail.
    public sealed class CoopTestClient : MonoBehaviour {
        private const float STEP_TIMEOUT_SECONDS = 120f;
        private const float SETTLE_SECONDS = 1.5f;
        private const int LEAVE_LOOP = 2;
        private const int LAST_LOOP = 3;
        private const int FAILURE_EXIT_CODE = 1;
        private const float FULL_THROTTLE = 1f;
        private const float FULL_THROTTLE_TOLERANCE = 0.02f;
        private const float HELM_DROP_HEIGHT = 0.5f;
        // After its pull to Stop this client must see the ship at rest within the stop timeout (m/s, s).
        private const float STOPPED_SPEED = 0.2f;
        private const float STOP_TIMEOUT_SECONDS = 6f;
        private const float SPEED_SAMPLE_SECONDS = 0.5f;
        private const int PISTOL_SHOTS = 3;
        // Longer than the pistol's cooldown, so every click fires.
        private const float PISTOL_AIM_SECONDS = 2.2f;
        // The pilot steers this long into the cruise before it leaves, so the host sees the ship turn.
        private const float STEER_BEFORE_LEAVING_SECONDS = 0.5f;
        // Held on the move stick from the helm: D, a turn to the right.
        private static readonly Vector2 _steerRight = Vector2.right;

        private readonly WaitForSeconds _settle = new WaitForSeconds(SETTLE_SECONDS);
        private readonly WaitForSeconds _exchangePoll = new WaitForSeconds(CoopTestExchange.POLL_SECONDS);
        private readonly WaitForSeconds _speedSample = new WaitForSeconds(SPEED_SAMPLE_SECONDS);
        private readonly WaitForSeconds _steerBeforeLeaving = new WaitForSeconds(STEER_BEFORE_LEAVING_SECONDS);

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
            if (CoopWeaponsEconomyClientScenarios.TryCreate(_arguments.Scenario, this, _exchange, _arguments.ExchangeDirectory, out IEnumerator scenario)) {
                yield return scenario;
                yield break;
            }

            if (_arguments.Scenario != CoopTestSteps.SCENARIO_LEAVE_AND_REJOIN)
                yield return FailCoroutine($"Unknown scenario {_arguments.Scenario}.");

            yield return JoinLobbyCoroutine();
            yield return WaitForCoroutine(SessionQueries.IsGameAtStation, "GameScene at the start station");
            yield return BoardCoroutine();
            yield return RideLoopCoroutine(CoopTestSteps.HELM_LOOP);
            yield return TakeHelmCoroutine();
            yield return LeaveInFlightCoroutine();
            yield return KnockInFlightCoroutine();
            yield return RejoinAtStationCoroutine();
            yield return DieCoroutine();
            yield return WaitForBuyBackCoroutine();
            yield return FirePistolCoroutine();
            yield return PushThrottleCoroutine();
            yield return RideLoopCoroutine(LAST_LOOP);
            yield return FinishCoroutine();
        }

        internal IEnumerator JoinLobbyCoroutine() {
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

            if (loopIndex == CoopTestSteps.THROTTLE_LOOP)
                yield return StopAndGoThrottleInCruiseCoroutine();

            yield return WaitForCoroutine(() => SessionQueries.IsAtStationAfterLoop(loopIndex), $"Build after loop {loopIndex}");
            yield return _settle;
            Report(CoopObserver.CaptureGame(CoopTestSteps.LANDED));
        }

        // The host navigator marks a contact in this cruise: the mark must reach this client's synced sonar model.
        private IEnumerator WaitForSonarMarkCoroutine() {
            yield return WaitForCoroutine(() => SessionQueries.Resolve<IReadOnlySonarConsoleModel>().Marks.Count > 0, "the host navigator's sonar mark");
            Report(CoopObserver.CaptureGame(CoopTestSteps.SONAR_MARK_SEEN));
        }

        // Walks to the helm (put beside its seat), sits down through the seat's use command, what E sends, and holds D
        // through the scripted move stick until it leaves: the host checks the seat, then the turn in the next cruise.
        private IEnumerator TakeHelmCoroutine() {
            ShipBase ship = Object.FindAnyObjectByType<ShipBase>();
            ShipSocket helm = CoopHelm.FindSocket(ship);
            Vector3 sitSpot = ship.transform.InverseTransformPoint(helm.Seat.SitPoint.position);
            ShipDeckBoarding.PutAbove(SessionQueries.LocalPlayer.GetComponent<CharacterMovableBase>(), ship, sitSpot, HELM_DROP_HEIGHT);
            yield return _settle;

            SessionQueries.LocalPlayer.GetComponent<UseController>().RequestUse(helm.GetComponentInChildren<ShipSitInteractable>());
            yield return WaitForCoroutine(() => CoopHelm.FindStation(ship).IsSteeredByLocalPlayer, "the local player at the helm");
            CharacterInputBuffer input = SessionQueries.Resolve<CharacterInputBuffer>();
            input.BeginScripted();
            input.SetScripted(_steerRight, false, false);
            Report(CoopObserver.CaptureGame(CoopTestSteps.SEATED_AT_HELM));
        }

        private IEnumerator LeaveInFlightCoroutine() {
            yield return WaitForCruiseCoroutine(LEAVE_LOOP);
            yield return _steerBeforeLeaving;
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

        // The development-build kill, the same command as the debug window's "Kill me": the death itself is the server's.
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

        // On the host's command, with the pistol the host put in the hand: aims the view at the debug dummy through the
        // look input and clicks the fire button a few times, one cooldown apart; the host checks what arrived.
        private IEnumerator FirePistolCoroutine() {
            yield return WaitForCoroutine(() => _exchange.HasCommand(CoopTestSteps.COMMAND_FIRE_PISTOL), "the host's fire-pistol command", _exchangePoll);
            GrabController hand = NetworkClient.localPlayer.GetComponent<GrabController>();
            yield return WaitForCoroutine(() => hand.Held != null && hand.Held.TryGetComponent(out HandWeaponItem _), "the pistol in the hand");
            WeaponDebugTarget target = null;
            yield return WaitForCoroutine(() => (target = Object.FindAnyObjectByType<WeaponDebugTarget>()) != null, "the host's debug target");
            try {
                CameraLookDriver.DebugPitchOverride = true;
                for (int shot = 0; shot < PISTOL_SHOTS; shot++) {
                    for (float aimed = 0f; aimed < PISTOL_AIM_SECONDS; aimed += Time.deltaTime) {
                        HandWeaponAiming.KeepAiming(target.GetComponentInChildren<Collider>().bounds.center);
                        yield return null;
                    }

                    yield return HandWeaponAiming.ClickFireCoroutine();
                    if (SessionQueries.Resolve<HeldWeaponReadinessModel>().Readiness >= 1f)
                        yield return FailCoroutine($"A click on the ready pistol did not fire (cursor {Cursor.lockState}).");
                }
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            HandWeaponAiming.TurnViewBack();
            yield return null;

            Report(CoopObserver.CaptureGame(CoopTestSteps.PISTOL_FIRED));
        }

        // On the host's command: behind the throttle handle, hold E on it and push the mouse forward until the ship
        // takes off and the handle is at Full, then let go.
        private IEnumerator PushThrottleCoroutine() {
            yield return WaitForCoroutine(() => _exchange.HasCommand(CoopTestSteps.COMMAND_PUSH_THROTTLE), "the host's push-throttle command", _exchangePoll);
            ShipThrottleHandling handling = new ShipThrottleHandling(Object.FindAnyObjectByType<ShipBase>());
            yield return handling.PushHandleCoroutine(IsTakenOffAtFullThrottle);
            if (handling.Failure != null)
                yield return FailCoroutine($"Pushing the throttle handle failed: {handling.Failure}.");

            Report(CoopObserver.CaptureGame(CoopTestSteps.THROTTLE_PUSHED));
        }

        // On the host's commands in the last cruise: pull the held handle back to Stop, then push it to Full again.
        private IEnumerator StopAndGoThrottleInCruiseCoroutine() {
            yield return WaitForCoroutine(() => _exchange.HasCommand(CoopTestSteps.COMMAND_PULL_THROTTLE), "the host's pull-throttle command", _exchangePoll);
            ShipThrottleHandling handling = new ShipThrottleHandling(Object.FindAnyObjectByType<ShipBase>());
            yield return handling.PullHandleCoroutine(() => ReadThrottlePosition() <= FULL_THROTTLE_TOLERANCE);
            if (handling.Failure != null)
                yield return FailCoroutine($"Pulling the throttle handle to Stop in cruise failed: {handling.Failure}.");

            yield return WaitForShipSeenStoppedCoroutine();
            Report(CoopObserver.CaptureGame(CoopTestSteps.THROTTLE_PULLED));
            yield return WaitForCoroutine(() => _exchange.HasCommand(CoopTestSteps.COMMAND_PUSH_THROTTLE_IN_CRUISE), "the host's push-throttle-in-cruise command", _exchangePoll);
            yield return handling.PushHandleCoroutine(() => ReadThrottlePosition() >= FULL_THROTTLE - FULL_THROTTLE_TOLERANCE);
            if (handling.Failure != null)
                yield return FailCoroutine($"Pushing the throttle handle to Full in cruise failed: {handling.Failure}.");

            Report(CoopObserver.CaptureGame(CoopTestSteps.THROTTLE_PUSHED_IN_CRUISE));
        }

        // The ship as this client sees it (its interpolated pose), not the server's.
        private IEnumerator WaitForShipSeenStoppedCoroutine() {
            Transform ship = Object.FindAnyObjectByType<ShipBase>().transform;
            float deadline = Time.realtimeSinceStartup + STOP_TIMEOUT_SECONDS;
            while (true) {
                Vector3 start = ship.position;
                yield return _speedSample;
                Vector3 moved = ship.position - start;
                moved.y = 0f;
                float speed = moved.magnitude / SPEED_SAMPLE_SECONDS;
                if (speed < STOPPED_SPEED)
                    yield break;

                if (Time.realtimeSinceStartup > deadline)
                    yield return FailCoroutine($"This client still sees the ship move at {speed:0.00} m/s {STOP_TIMEOUT_SECONDS} s after its pull to Stop (synced throttle {ReadThrottlePosition():0.000}).");
            }
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

        private static bool IsTakenOffAtFullThrottle() =>
            RunModel().Phase != ShipRunPhase.Build && ReadThrottlePosition() >= FULL_THROTTLE - FULL_THROTTLE_TOLERANCE;

        private static float ReadThrottlePosition() =>
            SessionQueries.Resolve<IReadOnlyShipThrottleModel>().Position;

        private static IReadOnlyShipRunModel RunModel() =>
            SessionQueries.Resolve<IReadOnlyShipRunModel>();

        // pause: what to wait between checks; null checks every frame.
        internal IEnumerator WaitForCoroutine(Func<bool> condition, string target, YieldInstruction pause = null) {
            float deadline = Time.realtimeSinceStartup + STEP_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    yield return FailCoroutine($"Timed out after {STEP_TIMEOUT_SECONDS} s waiting for {target}.");

                yield return pause;
            }
        }

        // Reports the failure and quits; never returns, so no later step reports anything.
        internal IEnumerator FailCoroutine(string message) {
            CoopObservation observation = CoopObserver.CaptureStep(CoopTestSteps.FAILED);
            observation.Message = message;
            observation.Errors = CollectErrors();
            Report(observation);
            _hasFailed = true;
            Application.Quit(FAILURE_EXIT_CODE);
            while (true)
                yield return null;
        }

        internal string[] CollectErrors() =>
            _errorLog.Count == 0 ? Array.Empty<string>() : new[] { _errorLog.Describe() };

        internal void Report(CoopObservation observation) {
            if (_hasFailed)
                return;

            _exchange.AppendReport(observation);
            Debug.Log($"[CoopClient] {observation.Describe()}");
        }
    }
}
#endif
