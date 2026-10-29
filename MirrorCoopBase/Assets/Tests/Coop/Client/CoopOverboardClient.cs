#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using Features.DebugModule.Contracts;
using Features.ShipModule.Scripts.Overboard;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;

namespace Tests.Coop.Client {
    // The client falls overboard twice (scenario overboard): on the host's command it clicks its own debug window's
    // "Throw me overboard" as a player does, waits until the server put it overboard and its body drifts, and reports
    // with a screenshot; on the next command it waits until it rides the deck again (the host brought the ship the
    // first time and pulled it in with the harpoon the second) and reports that.
    public sealed class CoopOverboardClient : CoopScenarioClientBot {
        private const int LOOP = 1;
        private const string THROW_LABEL = "Throw me overboard";
        private const float DRIFT_SHOWN_SECONDS = 1.5f;

        private readonly DebugWindowClicking _debugWindow = new DebugWindowClicking();
        private readonly WaitForSeconds _driftShown = new WaitForSeconds(DRIFT_SHOWN_SECONDS);

        private static ShipOverboardRider Overboard => SessionQueries.LocalPlayer.GetComponent<ShipOverboardRider>();

        protected override IEnumerator RunScenarioCoroutine() {
            yield return BoardCoroutine();
            yield return WaitForCruiseCoroutine(LOOP);
            yield return FallOverboardCoroutine(CoopFlightSteps.COMMAND_THROW_OVERBOARD, CoopFlightSteps.THROWN_OVERBOARD);
            yield return ReportBackOnDeckCoroutine(CoopFlightSteps.COMMAND_REPORT_BACK_ON_DECK, CoopFlightSteps.BACK_ON_DECK);
            yield return FallOverboardCoroutine(CoopFlightSteps.COMMAND_THROW_OVERBOARD_AGAIN, CoopFlightSteps.THROWN_OVERBOARD_AGAIN);
            yield return ReportBackOnDeckCoroutine(CoopFlightSteps.COMMAND_REPORT_PULLED_ON_DECK, CoopFlightSteps.PULLED_ON_DECK);
        }

        private IEnumerator FallOverboardCoroutine(string command, string step) {
            yield return WaitForCommandCoroutine(command);
            yield return WaitForCoroutine(() => Ship.Riders.IsLocalPlayerRiding(), "riding the deck before the throw");
            yield return _debugWindow.ClickCoroutine(DebugSections.PLAYER, THROW_LABEL);
            yield return FailIfCoroutine(_debugWindow.Failure, $"Clicking \"{THROW_LABEL}\"");
            yield return WaitForCoroutine(() => Overboard.IsOverboard, "the server putting this player overboard");
            yield return _driftShown;
            if (Overboard.IsOverboard == false || Ship.Riders.IsLocalPlayerRiding())
                yield return FailCoroutine("The thrown player stopped drifting by itself.");

            yield return ReportWithScreenshotCoroutine(step);
        }

        private IEnumerator ReportBackOnDeckCoroutine(string command, string step) {
            yield return WaitForCommandCoroutine(command);
            yield return WaitForCoroutine(() => Overboard.IsOverboard == false && Ship.Riders.IsLocalPlayerRiding(), "riding the deck again");
            yield return Settle;
            yield return ReportWithScreenshotCoroutine(step);
        }

        private IEnumerator ReportWithScreenshotCoroutine(string step) {
            string screenshot = null;
            yield return CaptureScreenCoroutine(step, path => screenshot = path);
            CoopObservation observation = CoopFlightObserver.CaptureFlight(step);
            observation.Screenshot = screenshot;
            Report(observation);
        }
    }
}
#endif
