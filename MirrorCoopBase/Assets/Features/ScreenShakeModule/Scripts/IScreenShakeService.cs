namespace Features.ScreenShakeModule {
    // Shakes this peer's camera. Every shake runs out its own time, but only the strongest one at each moment is shown,
    // so a small shake during a big one adds nothing and a big one never weakens a small one that outlasts it.
    public interface IScreenShakeService {
        public void Shake(ScreenShakeProfile profile, float strength = 1f);
    }
}
