namespace Features.CameraModule.Contracts {
    // The one way to shake the local camera. All active shakes add up and are applied to the view once per frame.
    public interface ICameraShakeService {
        // A shake that fades out by itself. While a shake of the same profile is still stronger, a weaker one is ignored.
        public void AddShake(CameraShakeProfile profile, float strength);

        // A shake that lasts until its handle stops it; its intensity starts at 0.
        public ICameraShakeHandle BeginShake(CameraShakeProfile profile);
    }
}
