using Features.MvpModule;
using Features.ShipModule.Scripts;

namespace Features.LobbyModule.Scripts {
    public sealed class RadarPresenter : PresenterBehaviour<RadarViewBase> {
        private readonly ShipRadarService _radar;

        public RadarPresenter(ShipRadarService radar) {
            _radar = radar;
        }

        protected override void OnViewSet() {
            if (View != null && View.Slot != null)
                _radar.Attach(View.Slot);
        }

        protected override void OnDisposed() {
            _radar.Detach();
        }
    }
}
