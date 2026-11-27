using System;

namespace Features.ConstellationModule.Data {
    // Which constellation of the catalog this peer's sky shows and how far its reveal is, plus the one fading out after
    // a switch. Local and unsynced. Reveal seconds start below zero when the reveal waits for the old figure's fade.
    public sealed class ConstellationRevealModel {
        public const int NO_CONSTELLATION = -1;

        public int ShownConstellation { get; private set; } = NO_CONSTELLATION;
        public float ShownRevealSeconds { get; private set; }
        public int FadingConstellation { get; private set; } = NO_CONSTELLATION;
        public float FadingRevealSeconds { get; private set; }
        public float FadingSeconds { get; private set; }
        public bool HasShown => ShownConstellation != NO_CONSTELLATION;
        public bool HasFading => FadingConstellation != NO_CONSTELLATION;

        public event Action OnFiguresChanged;

        public void Show(int constellation, float revealDelaySeconds) {
            StartFadingShown();
            ShownConstellation = constellation;
            ShownRevealSeconds = -revealDelaySeconds;
            OnFiguresChanged?.Invoke();
        }

        public void Hide() {
            if (HasShown == false)
                return;

            StartFadingShown();
            ShownConstellation = NO_CONSTELLATION;
            OnFiguresChanged?.Invoke();
        }

        public void Advance(float deltaSeconds) {
            if (HasShown)
                ShownRevealSeconds += deltaSeconds;
            if (HasFading)
                FadingSeconds += deltaSeconds;
        }

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

        // The fading figure keeps the reveal it had reached, so a figure switched away mid-reveal fades as it was.
        private void StartFadingShown() {
            if (HasShown == false)
                return;

            FadingConstellation = ShownConstellation;
            FadingRevealSeconds = ShownRevealSeconds;
            FadingSeconds = 0f;
        }
    }
}
