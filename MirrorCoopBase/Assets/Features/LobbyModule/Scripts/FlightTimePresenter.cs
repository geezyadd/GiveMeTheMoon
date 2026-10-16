using Features.MvpModule;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using UnityEngine;

namespace Features.LobbyModule.Scripts {
    public sealed class FlightTimePresenter : PresenterBehaviour<FlightTimeViewBase> {
        private const string UNKNOWN_TEXT = "to platform —";
        private const string TIME_FORMAT = "to platform {0}:{1:00}";
        private const string DISTANCE_FORMAT = "to platform {0} m";
        private const int SECONDS_PER_MINUTE = 60;

        private readonly IReadOnlyShipRunModel _run;

        public FlightTimePresenter(IReadOnlyShipRunModel run) {
            _run = run;
        }

        protected override void OnViewSet() {
            View.OnTick += Refresh;
            Refresh();
        }

        protected override void OnDisposed() {
            View.OnTick -= Refresh;
        }

        private void Refresh() {
            if (View == null || View.IsViewDisposed)
                return;

            View.SetTimeText(Format(_run));
        }

        private static string Format(IReadOnlyShipRunModel run) {
            if (run == null || run.Phase == ShipRunPhase.Wreck)
                return UNKNOWN_TEXT;

            if (IsMovingAwayInFlight(run))
                return FormatDistance(run);

            float seconds = run.TransitSecondsRemaining;
            if (float.IsInfinity(seconds) || float.IsNaN(seconds))
                return UNKNOWN_TEXT;

            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return string.Format(TIME_FORMAT, total / SECONDS_PER_MINUTE, total % SECONDS_PER_MINUTE);
        }

        // The nose points away from B: a time at cruise speed would only grow, the distance tells the pilot more.
        private static bool IsMovingAwayInFlight(IReadOnlyShipRunModel run) =>
            run.Phase == ShipRunPhase.Cruise && run.TransitAlignment < 0f;

        // In flight the route work is the horizontal distance to B in meters (ShipRoute.ApplyFrame).
        private static string FormatDistance(IReadOnlyShipRunModel run) =>
            string.Format(DISTANCE_FORMAT, Mathf.Max(0, Mathf.RoundToInt(run.TransitWorkRemaining)));
    }
}
