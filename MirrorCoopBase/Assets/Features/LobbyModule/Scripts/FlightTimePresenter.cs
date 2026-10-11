using Features.MvpModule;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using UnityEngine;

namespace Features.LobbyModule.Scripts {
    public sealed class FlightTimePresenter : PresenterBehaviour<FlightTimeViewBase> {
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
                return "to platform —";

            float seconds = run.TransitSecondsRemaining;
            if (float.IsInfinity(seconds) || float.IsNaN(seconds))
                return "to platform —";

            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = total / 60;
            int remainder = total % 60;
            return $"to platform {minutes}:{remainder:00}";
        }
    }
}
