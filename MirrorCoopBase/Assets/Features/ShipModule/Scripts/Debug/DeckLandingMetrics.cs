#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts.Debug {
    // One landing after a DeckFeelHarness jump: sag below and overshoot above the stand height, and the settle time.
    internal struct DeckLandingMetrics {
        public float Sag;
        public float Overshoot;
        public float SettleSeconds;

        public string Text() =>
            Sag.ToString("0.000") + " / " + Overshoot.ToString("0.000") + " / " + SettleSeconds.ToString("0.00");
    }
}
#endif
