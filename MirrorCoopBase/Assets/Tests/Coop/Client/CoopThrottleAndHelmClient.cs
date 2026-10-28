#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShipModule.Scripts.Throttle;
using Mirror;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // The client flies the ship (scenario throttle-and-helm): takes off by pushing the throttle handle past the mark,
    // pulls it to Stop in the cruise and pushes it to Full again; at the next station it sits at the helm, steers the
    // next cruise right and left, tries the throttle from the seat (it must not get it), reports the ship's pose and
    // stands up. Each step on the host's command.
    public sealed class CoopThrottleAndHelmClient : CoopScenarioClientBot {
        private const int THROTTLE_LOOP = 1;
        private const int HELM_LOOP = 2;
        private const float FULL_THROTTLE = 1f;
        private const float THROTTLE_TOLERANCE = 0.02f;
        private const float HELM_DROP_HEIGHT = 0.5f;
        private const float STOPPED_SPEED = 0.2f;
        private const float STOP_TIMEOUT_SECONDS = 6f;
        private const float SPEED_SAMPLE_SECONDS = 0.5f;
        private const float STEER_RIGHT_SECONDS = 1.5f;
        // Longer than the turn right, so the ship ends up left of where the pilot started.
        private const float STEER_LEFT_SECONDS = 3f;
        private const float TRY_THROTTLE_SECONDS = 1f;
        // Mouse look per frame while trying to push the handle from the seat (pixels).
        private const float PUSH_LOOK = 12f;
        private const int JUMP_FRAMES = 3;

        private readonly WaitForSeconds _speedSample = new WaitForSeconds(SPEED_SAMPLE_SECONDS);
        private readonly WaitForSeconds _steerRight = new WaitForSeconds(STEER_RIGHT_SECONDS);
        private readonly WaitForSeconds _steerLeft = new WaitForSeconds(STEER_LEFT_SECONDS);

        private static NetworkIdentity Player => SessionQueries.LocalPlayer;
        private static IReadOnlyShipThrottleModel Throttle => SessionQueries.Resolve<IReadOnlyShipThrottleModel>();
        private static CharacterInputBuffer Input => SessionQueries.Resolve<CharacterInputBuffer>();

        protected override IEnumerator RunScenarioCoroutine() {
            yield return BoardCoroutine();
            yield return PushThrottleToTakeOffCoroutine();
            yield return WaitForCruiseCoroutine(THROTTLE_LOOP);
            yield return StopAndGoInCruiseCoroutine();
            yield return WaitForLandingCoroutine(THROTTLE_LOOP);
            yield return TakeHelmCoroutine();
            yield return WaitForCruiseCoroutine(HELM_LOOP);
            yield return SteerRightAndLeftCoroutine();
            yield return TryThrottleFromSeatCoroutine();
            yield return ReportPoseCoroutine();
            yield return StandUpCoroutine();
            yield return WaitForLandingCoroutine(HELM_LOOP);
        }

        private IEnumerator PushThrottleToTakeOffCoroutine() {
            yield return WaitForCommandCoroutine(CoopTestSteps.COMMAND_PUSH_THROTTLE);
            ShipThrottleHandling handling = new ShipThrottleHandling(Ship);
            yield return handling.PushHandleCoroutine(IsTakenOffAtFullThrottle);
            yield return FailIfCoroutine(handling.Failure, "Pushing the throttle handle to take off");
            Report(CoopFlightObserver.CaptureFlight(CoopTestSteps.THROTTLE_PUSHED));
        }

        private IEnumerator StopAndGoInCruiseCoroutine() {
            yield return WaitForCommandCoroutine(CoopTestSteps.COMMAND_PULL_THROTTLE);
            ShipThrottleHandling handling = new ShipThrottleHandling(Ship);
            yield return handling.PullHandleCoroutine(() => Throttle.Position <= THROTTLE_TOLERANCE);
            yield return FailIfCoroutine(handling.Failure, "Pulling the throttle handle to Stop in cruise");
            yield return WaitForShipSeenStoppedCoroutine();
            Report(CoopFlightObserver.CaptureFlight(CoopTestSteps.THROTTLE_PULLED));

            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_REPORT_STOPPED);
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.SHIP_STOPPED));

            yield return WaitForCommandCoroutine(CoopTestSteps.COMMAND_PUSH_THROTTLE_IN_CRUISE);
            yield return handling.PushHandleCoroutine(() => Throttle.Position >= FULL_THROTTLE - THROTTLE_TOLERANCE);
            yield return FailIfCoroutine(handling.Failure, "Pushing the throttle handle to Full in cruise");
            Report(CoopFlightObserver.CaptureFlight(CoopTestSteps.THROTTLE_PUSHED_IN_CRUISE));
        }

        // The ship as this client sees it (its interpolated pose), not the server's.
        private IEnumerator WaitForShipSeenStoppedCoroutine() {
            Transform ship = Ship.transform;
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
                    yield return FailCoroutine($"This client still sees the ship move at {speed:0.00} m/s {STOP_TIMEOUT_SECONDS} s after its pull to Stop (synced throttle {Throttle.Position:0.000}).");
            }
        }

        // Put beside the helm's seat, sits down through the seat's use command, what E sends.
        private IEnumerator TakeHelmCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_TAKE_HELM);
            ShipBase ship = Ship;
            ShipSocket helm = CoopHelm.FindSocket(ship);
            Vector3 sitSpot = ship.transform.InverseTransformPoint(helm.Seat.SitPoint.position);
            ShipDeckBoarding.PutAbove(Player.GetComponent<CharacterMovableBase>(), ship, sitSpot, HELM_DROP_HEIGHT);
            yield return Settle;

            Player.GetComponent<UseController>().RequestUse(helm.GetComponentInChildren<ShipSitInteractable>());
            yield return WaitForCoroutine(() => CoopHelm.FindStation(ship).IsSteeredByLocalPlayer, "the local player at the helm");
            Report(CoopFlightObserver.CaptureFlight(CoopTestSteps.SEATED_AT_HELM));
        }

        // D, then A for longer, through the scripted move stick the keyboard fills; then the stick is let go.
        private IEnumerator SteerRightAndLeftCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_STEER);
            Input.BeginScripted();
            Input.SetScripted(Vector2.right, false, false);
            yield return _steerRight;
            Input.SetScripted(Vector2.left, false, false);
            yield return _steerLeft;
            Input.SetScripted(Vector2.zero, false, false);
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.STEERED));
        }

        // A seated pilot cannot reach the handle: the aim does not offer it, and E with the mouse forward takes nothing.
        private IEnumerator TryThrottleFromSeatCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_TRY_THROTTLE_SEATED);
            ShipThrottleInteractable handle = Object.FindAnyObjectByType<ShipThrottleInteractable>();
            LocalPlayerInteraction interaction = Player.GetComponent<LocalPlayerInteraction>();
            if (handle.CanUse(Player, Player.GetComponent<GrabController>()))
                yield return FailCoroutine("The seated pilot may use the throttle handle.");

            string playerKey = Player.GetComponent<ShipRider>().Key.Id;
            bool hasHeld = false;
            float positionBefore = Throttle.Position;
            try {
                CameraLookDriver.DebugPitchOverride = true;
                CameraLookDriver.DebugPitch = Vector3.SignedAngle(Player.transform.forward, handle.transform.position - Player.transform.position, Player.transform.right);
                yield return null;
                yield return null;
                if (interaction.Target.Usable == handle)
                    yield return FailCoroutine("The aim offers the throttle handle to the seated pilot.");

                // Another usable under the crosshair (the helm itself) would take E: only an empty aim is tried.
                bool isAimFree = interaction.Target.Usable == null;
                if (isAimFree)
                    interaction.Interact();

                for (float time = 0f; time < TRY_THROTTLE_SECONDS; time += Time.deltaTime) {
                    interaction.MoveHeldControl(Vector2.up * PUSH_LOOK);
                    hasHeld |= Throttle.HolderKey == playerKey;
                    yield return null;
                }

                if (isAimFree)
                    interaction.EndInteract();
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            if (hasHeld)
                yield return FailCoroutine("The server gave the throttle handle to the seated pilot.");

            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.THROTTLE_REFUSED);
            observation.Message = $"throttle {positionBefore:0.000} before the try";
            Report(observation);
        }

        private IEnumerator ReportPoseCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_REPORT_POSE);
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.POSE));
        }

        // The jump stands a seated rider up.
        private IEnumerator StandUpCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_STAND_UP);
            Input.SetScripted(Vector2.zero, true, false);
            for (int frame = 0; frame < JUMP_FRAMES; frame++)
                yield return null;

            Input.SetScripted(Vector2.zero, false, false);
            yield return WaitForCoroutine(() => CoopHelm.FindStation(Ship).IsSteeredByLocalPlayer == false, "the local player off the helm");
            Input.EndScripted();
            yield return Settle;
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.STOOD_UP);
            observation.IsSeated = Player.GetComponent<ShipRider>().IsSeated;
            observation.IsRiding = Player.GetComponent<ShipRider>().IsRiding;
            Report(observation);
        }

        private static bool IsTakenOffAtFullThrottle() =>
            RunModel.Phase != ShipRunPhase.Build && Throttle.Position >= FULL_THROTTLE - THROTTLE_TOLERANCE;
    }
}
#endif
