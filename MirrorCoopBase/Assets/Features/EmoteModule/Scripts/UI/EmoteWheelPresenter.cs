using Features.EmoteModule.Data;
using Features.MvpModule;

namespace Features.EmoteModule.UI {
    // The emote wheel: a sector per catalog emote, the one under the pointer highlighted.
    public sealed class EmoteWheelPresenter : PresenterBehaviour<EmoteWheelViewBase> {
        private readonly EmoteWheelModel _wheelModel;
        private readonly EmoteCatalog _catalog;

        public EmoteWheelPresenter(EmoteWheelModel wheelModel, EmoteCatalog catalog) {
            _wheelModel = wheelModel;
            _catalog = catalog;
        }

        protected override void OnViewSet() {
            View.ShowEmotes(_catalog.Emotes);
            _wheelModel.OnHighlightChanged += OnHighlightChanged;
            OnHighlightChanged();
        }

        protected override void OnDisposed() =>
            _wheelModel.OnHighlightChanged -= OnHighlightChanged;

        private void OnHighlightChanged() =>
            View.Highlight(_wheelModel.HighlightedIndex);
    }
}
