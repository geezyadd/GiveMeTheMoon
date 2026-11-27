using System;

namespace Features.ConstellationModule.Data {
    // Which catalog constellation this peer's sky draws and how far its reveal is, plus the one fading out after a
    // switch. Filled from the synced ConstellationSky model; times are network times, so every peer's reveal is in step.
    // The reveal seconds are below zero while the reveal waits for the old figure's fade.
    public sealed class ConstellationRevealModel {
        public const int NO_CONSTELLATION = -1;

        private double _shownRevealStartTime;
        private double _fadeStartTime;
        private double _time;

        public int ShownConstellation { get; private set; } = NO_CONSTELLATION;
        public float ShownRevealSeconds => (float)(_time - _shownRevealStartTime);
        public int FadingConstellation { get; private set; } = NO_CONSTELLATION;
        public float FadingRevealSeconds { get; private set; }
        public float FadingSeconds => (float)(_time - _fadeStartTime);
        public bool HasShown => ShownConstellation != NO_CONSTELLATION;
        public bool HasFading => FadingConstellation != NO_CONSTELLATION;

        public event Action OnFiguresChanged;

        // The same reveal told again (the synced model raises its change once more when it becomes available) is
        // ignored, so it does not fade out and replay.
        public void Show(int constellation, double revealStartTime, double changeTime) {
            if (ShownConstellation == constellation && _shownRevealStartTime.Equals(revealStartTime))
                return;

            StartFadingShown(changeTime);
            ShownConstellation = constellation;
            _shownRevealStartTime = revealStartTime;
            OnFiguresChanged?.Invoke();
        }

        public void Hide(double changeTime) {
            if (HasShown == false)
                return;

            StartFadingShown(changeTime);
            ShownConstellation = NO_CONSTELLATION;
            OnFiguresChanged?.Invoke();
        }

        public void SetTime(double time) =>
            _time = time;

        public void EndFading() {
            if (HasFading == false)
                return;

            FadingConstellation = NO_CONSTELLATION;
            OnFiguresChanged?.Invoke();
        }

        public void Clear() {
            if (HasShown == false && HasFading == false)
                return;

            ShownConstellation = NO_CONSTELLATION;
            FadingConstellation = NO_CONSTELLATION;
            OnFiguresChanged?.Invoke();
        }

        // The fading figure keeps the reveal it had reached when it was switched away, so it fades as it was.
        private void StartFadingShown(double changeTime) {
            if (HasShown == false)
                return;

            FadingConstellation = ShownConstellation;
            FadingRevealSeconds = (float)(changeTime - _shownRevealStartTime);
            _fadeStartTime = changeTime;
        }
    }
}
