using Features.MvpModule;
using Features.SonarModule.Data;
using Features.SonarModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SonarModule.Scripts.UI {
    // The sonar window: the scope from the local scope model, the navigator's clicks as console requests.
    public sealed class SonarScopePresenter : PresenterBehaviour<SonarScopeViewBase> {
        private const string STATUS_FORMAT = "RANGE {0:N0} m    MARKS {1}/{2}    SPACE: ping    CLICK A CONTACT: mark    E / ESC: leave";
        private const string NO_SIGNAL_STATUS = "NO SIGNAL: the radar needs repair";

        private readonly SonarScopeModel _sonarScopeModel;
        private readonly SonarConsoleRequests _sonarConsoleRequests;
        private readonly SonarConfiguration _sonarConfiguration;

        private bool _hasShownStatus;
        private int _shownRangeMeters;
        private int _shownMarkCount;
        private bool _shownIsWorking;

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
            ShowStatusOnChange(frame);
        }

        // The status text is built only when what it says changes, not every frame.
        private void ShowStatusOnChange(SonarScopeFrame frame) {
            int rangeMeters = Mathf.RoundToInt(frame.RangeMeters);
            bool isStatusChanged = _hasShownStatus == false || rangeMeters != _shownRangeMeters || frame.MarkCount != _shownMarkCount || frame.IsWorking != _shownIsWorking;
            if (isStatusChanged == false)
                return;

            _hasShownStatus = true;
            _shownRangeMeters = rangeMeters;
            _shownMarkCount = frame.MarkCount;
            _shownIsWorking = frame.IsWorking;
            View.ShowStatus(frame.IsWorking ? string.Format(STATUS_FORMAT, rangeMeters, frame.MarkCount, frame.MarkLimit) : NO_SIGNAL_STATUS);
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
