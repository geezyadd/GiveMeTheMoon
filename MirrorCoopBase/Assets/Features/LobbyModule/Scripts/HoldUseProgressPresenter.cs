using Features.GrabModule.Scripts.Hold;
using Features.MvpModule;

namespace Features.LobbyModule.Scripts {
    // The local player's hold (a repair with a tool) as a bar under the crosshair, while the hold runs.
    public sealed class HoldUseProgressPresenter : PresenterBehaviour<HoldUseProgressViewBase> {
        private readonly HoldUseModel _holdUseModel;

        public HoldUseProgressPresenter(HoldUseModel holdUseModel) =>
            _holdUseModel = holdUseModel;

        protected override void OnViewSet() {
            _holdUseModel.OnProgressChanged += Refresh;
            Refresh();
        }

        protected override void OnDisposed() =>
            _holdUseModel.OnProgressChanged -= Refresh;

        private void Refresh() {
            View.SetProgressVisible(_holdUseModel.IsHolding);
            View.SetProgress(_holdUseModel.Progress);
        }
    }
}
