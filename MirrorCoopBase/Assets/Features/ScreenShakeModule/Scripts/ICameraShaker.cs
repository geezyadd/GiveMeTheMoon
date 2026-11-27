namespace Features.ScreenShakeModule {
    // A camera that shows the screen shake.
    public interface ICameraShaker {
        public void Apply(float amplitude, float frequency);
    }
}
