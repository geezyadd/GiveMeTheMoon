using Features.MvpModule;

namespace Features.PlayerLifeModule.Scripts.Spectator {
    public sealed class SpectatorHudPresenter : PresenterBehaviour<SpectatorHudViewBase> {
        private readonly SpectatorModel _spectatorModel;

        public SpectatorHudPresenter(SpectatorModel spectatorModel) {
            _spectatorModel = spectatorModel;
        }

        protected override void OnViewSet() {
            _spectatorModel.OnChanged += Refresh;
            Refresh();
        }

        protected override void OnDisposed() {
            _spectatorModel.OnChanged -= Refresh;
        }

        private void Refresh() {
            if (View.IsViewDisposed)
                return;

            if (_spectatorModel.IsSpectating == false) {
                View.HideView();
                return;
            }

            View.SetLabel(_spectatorModel.Label);
            View.ShowView();
        }
    }
}
