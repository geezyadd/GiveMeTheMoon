using Features.MvpModule;
using Features.SonarModule.Data;

namespace Features.SonarModule.Scripts.UI {
    // Every player's world markers of the crew's marks, as laid out for this frame.
    public sealed class SonarMarkersPresenter : PresenterBehaviour<SonarMarkersViewBase> {
        private readonly SonarMarkerScreenModel _sonarMarkerScreenModel;

        public SonarMarkersPresenter(SonarMarkerScreenModel sonarMarkerScreenModel) =>
            _sonarMarkerScreenModel = sonarMarkerScreenModel;

        protected override void OnViewSet() {
            _sonarMarkerScreenModel.OnMarkersChanged += OnMarkersChanged;
            OnMarkersChanged();
        }

        protected override void OnDisposed() =>
            _sonarMarkerScreenModel.OnMarkersChanged -= OnMarkersChanged;

        private void OnMarkersChanged() =>
            View.ShowMarkers(_sonarMarkerScreenModel.Markers);
    }
}
