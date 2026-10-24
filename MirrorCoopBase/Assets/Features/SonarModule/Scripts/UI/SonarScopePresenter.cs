using Features.MvpModule;
using Features.SonarModule.Data;
using Features.SonarModule.Scripts.Configurations;

namespace Features.SonarModule.Scripts.UI {
    // The sonar window: the scope from the local scope model, the navigator's clicks as console requests.
    public sealed class SonarScopePresenter : PresenterBehaviour<SonarScopeViewBase> {
        private const string STATUS_FORMAT = "RANGE {0:N0} m    MARKS {1}/{2}    SPACE: ping    CLICK A CONTACT: mark    E / ESC: leave";
        private const string NO_SIGNAL_STATUS = "NO SIGNAL: the radar needs repair";

        private readonly SonarScopeModel _sonarScopeModel;
        private readonly SonarConsoleRequests _sonarConsoleRequests;
        private readonly SonarConfiguration _sonarConfiguration;

        public SonarScopePresenter(SonarScopeModel sonarScopeModel, SonarConsoleRequests sonarConsoleRequests, SonarConfiguration sonarConfiguration) {
            _sonarScopeModel = sonarScopeModel;
            _sonarConsoleRequests = sonarConsoleRequests;
            _sonarConfiguration = sonarConfiguration;
        }

        protected override void OnViewSet() {
            View.SetRings(_sonarConfiguration.RangeRingCount);
            View.OnPingClicked += OnPingClicked;
            View.OnCloseClicked += OnCloseClicked;
            View.OnBlipClicked += OnBlipClicked;
            _sonarScopeModel.OnFrameChanged += OnFrameChanged;
            _sonarScopeModel.OnPinged += OnPinged;
            OnFrameChanged();
        }

        protected override void OnDisposed() {
            View.OnPingClicked -= OnPingClicked;
            View.OnCloseClicked -= OnCloseClicked;
            View.OnBlipClicked -= OnBlipClicked;
            _sonarScopeModel.OnFrameChanged -= OnFrameChanged;
            _sonarScopeModel.OnPinged -= OnPinged;
        }

        private void OnFrameChanged() {
            SonarScopeFrame frame = _sonarScopeModel.Frame;
            View.ShowBlips(_sonarScopeModel.Blips);
            View.ShowWave(frame.WaveShare);
            View.ShowCooldown(frame.CooldownShare);
            View.ShowNoise(frame.IsWorking == false);
            View.ShowStatus(frame.IsWorking ? string.Format(STATUS_FORMAT, frame.RangeMeters, frame.MarkCount, frame.MarkLimit) : NO_SIGNAL_STATUS);
        }

        private void OnPinged() =>
            View.PlayPing();

        private void OnPingClicked() =>
            _sonarConsoleRequests.RequestPing();

        private void OnCloseClicked() =>
            _sonarConsoleRequests.RequestClose();

        private void OnBlipClicked(string contactId) =>
            _sonarConsoleRequests.RequestMark(contactId);
    }
}
