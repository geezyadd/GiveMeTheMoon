using System;
using System.Threading;
using System.Threading.Tasks;
using Features.SettingsModule.Data;
using UnityEngine;

namespace Features.SettingsModule.UI {
    // After the window mode or resolution changed: counts the seconds down and puts the old display values back into
    // the live model and the draft unless the player keeps them in time.
    public sealed class DisplaySettingsConfirmation {
        private const int MILLISECONDS_PER_SECOND = 1000;

        private readonly SettingsModel _liveModel;
        private readonly SettingsModel _draft;

        private CancellationTokenSource _countdown;
        private string _previousWindowMode;
        private string _previousResolution;

        public DisplaySettingsConfirmation(SettingsModel liveModel, SettingsModel draft) {
            _liveModel = liveModel;
            _draft = draft;
        }

        public bool IsRunning => _countdown != null;

        public static bool DiffersInDisplay(SettingsModel model, SettingsModel other) =>
            model.HasSameValue(other, SettingKeys.WINDOW_MODE) == false
            || model.HasSameValue(other, SettingKeys.RESOLUTION) == false;

        public void Remember() {
            _previousWindowMode = _liveModel.GetChoice(SettingKeys.WINDOW_MODE);
            _previousResolution = _liveModel.GetChoice(SettingKeys.RESOLUTION);
        }

        // onSecond gets the seconds left; onFinished runs after keep, revert or the end of the time.
        public void Start(int seconds, Action<int> onSecond, Action onFinished) {
            Stop();
            _countdown = new CancellationTokenSource();
            _ = CountDownAsync(seconds, onSecond, onFinished, _countdown.Token);
        }

        public void Keep() =>
            Stop();

        public void Revert() {
            Stop();
            _liveModel.SetChoice(SettingKeys.WINDOW_MODE, _previousWindowMode);
            _liveModel.SetChoice(SettingKeys.RESOLUTION, _previousResolution);
            _draft.CopyValueFrom(_liveModel, SettingKeys.WINDOW_MODE);
            _draft.CopyValueFrom(_liveModel, SettingKeys.RESOLUTION);
        }

        private void Stop() {
            _countdown?.Cancel();
            _countdown?.Dispose();
            _countdown = null;
        }

        private async Task CountDownAsync(int seconds, Action<int> onSecond, Action onFinished, CancellationToken token) {
            try {
                for (int left = seconds; left > 0; left--) {
                    onSecond(left);
                    await Task.Delay(MILLISECONDS_PER_SECOND, token);
                }

                Revert();
                onFinished();
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) {
                Debug.LogException(exception);
            }
        }
    }
}
