using Features.MvpModule;
using Features.SonarModule.Data;
using Features.SonarModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SonarModule.Scripts.UI {
    // The sonar window: the scope from the local scope model, the navigator's clicks as console requests. Texts are
    // built only when what they say changes, not every frame.
    public sealed class SonarScopePresenter : PresenterBehaviour<SonarScopeViewBase> {
        private const string STATUS_FORMAT = "RANGE {0:N0} m    MARKS {1}/{2}    E / ESC: leave";
        private const string NO_SIGNAL_STATUS = "NO SIGNAL: the radar needs repair";
        private const string HINT = "PING (Space): a wave lights up everything around the ship.   CLICK a lit contact: mark it, the crew sees the mark through the haze.";
        private const string RING_LABEL_FORMAT = "{0:N0} m";
        private const string HEADING_FORMAT = "HEADING {0:000}\u00B0";
        private const string STATION_DISTANCE_FORMAT = "STATION {0:0.0} km";
        private const float METERS_PER_KILOMETER = 1000f;
        private const float STATION_DISTANCE_STEP_METERS = 100f;
        private const float FULL_TURN_DEGREES = 360f;

        private readonly SonarScopeModel _sonarScopeModel;
        private readonly SonarConsoleRequests _sonarConsoleRequests;
        private readonly SonarConfiguration _sonarConfiguration;

        private bool _hasShownStatus;
        private int _shownRangeMeters;
        private int _shownMarkCount;
        private bool _shownIsWorking;
        private int _shownRingRangeMeters = -1;
        private int _shownHeadingDegrees = -1;
        private int _shownStationSteps = -1;

        public SonarScopePresenter(SonarScopeModel sonarScopeModel, SonarConsoleRequests sonarConsoleRequests, SonarConfiguration sonarConfiguration) {
            _sonarScopeModel = sonarScopeModel;
            _sonarConsoleRequests = sonarConsoleRequests;
            _sonarConfiguration = sonarConfiguration;
        }

        protected override void OnViewSet() {
            View.SetRings(_sonarConfiguration.RangeRingCount);
            View.ShowLegend(_sonarConfiguration.Kinds);
            View.ShowHint(HINT);
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
            View.ShowNorth(frame.NorthScopePosition);
            bool isStationShown = frame.IsWorking && frame.HasStation;
            View.ShowStationBearing(isStationShown, frame.StationScopePosition);
            ShowStatusOnChange(frame);
            ShowRingLabelsOnChange(frame.RangeMeters);
            ShowHeadingOnChange(frame.HeadingDegrees);
            if (isStationShown)
                ShowStationDistanceOnChange(frame.StationDistanceMeters);
        }

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

        private void ShowRingLabelsOnChange(float rangeMeters) {
            int roundedRange = Mathf.RoundToInt(rangeMeters);
            if (roundedRange == _shownRingRangeMeters)
                return;

            _shownRingRangeMeters = roundedRange;
            int ringCount = _sonarConfiguration.RangeRingCount;
            for (int i = 0; i < ringCount; i++)
                View.ShowRingLabel(i, string.Format(RING_LABEL_FORMAT, rangeMeters * (i + 1) / ringCount));
        }

        private void ShowHeadingOnChange(float headingDegrees) {
            int roundedHeading = Mathf.RoundToInt(headingDegrees) % Mathf.RoundToInt(FULL_TURN_DEGREES);
            if (roundedHeading == _shownHeadingDegrees)
                return;

            _shownHeadingDegrees = roundedHeading;
            View.ShowHeading(string.Format(HEADING_FORMAT, roundedHeading));
        }

        private void ShowStationDistanceOnChange(float distanceMeters) {
            int steps = Mathf.RoundToInt(distanceMeters / STATION_DISTANCE_STEP_METERS);
            if (steps == _shownStationSteps)
                return;

            _shownStationSteps = steps;
            View.ShowStationDistance(string.Format(STATION_DISTANCE_FORMAT, steps * STATION_DISTANCE_STEP_METERS / METERS_PER_KILOMETER));
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
