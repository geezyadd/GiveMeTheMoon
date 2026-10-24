namespace Features.CameraModule.Contracts {
    // A continuous shake its source drives: the intensity eases to the set value over the profile's seconds.
    public interface ICameraShakeHandle {
        public void SetIntensity(float intensity);

        public void Stop();
    }
}
