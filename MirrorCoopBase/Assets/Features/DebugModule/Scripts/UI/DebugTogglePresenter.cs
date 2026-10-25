using Features.DebugModule.Scripts.Models;
using Features.MvpModule;

namespace Features.DebugModule.Scripts.UI {
    public sealed class DebugTogglePresenter : PresenterBehaviour<DebugToggleViewBase> {
        private readonly DebugPanelModel _debugPanelModel;

        public DebugTogglePresenter(DebugPanelModel debugPanelModel) =>
            _debugPanelModel = debugPanelModel;

        protected override void OnViewSet() {
            View.OnClicked += OnClicked;
            _debugPanelModel.OnOpenChanged += OnPanelOpenChanged;
            OnPanelOpenChanged();
        }

        protected override void OnDisposed() {
            View.OnClicked -= OnClicked;
            _debugPanelModel.OnOpenChanged -= OnPanelOpenChanged;
        }

        private void OnClicked() =>
            _debugPanelModel.Toggle();

        private void OnPanelOpenChanged() =>
            View.ShowPanelOpen(_debugPanelModel.IsOpen);
    }
}
