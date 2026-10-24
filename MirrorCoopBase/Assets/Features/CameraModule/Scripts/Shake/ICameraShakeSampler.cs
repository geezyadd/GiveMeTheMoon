namespace Features.CameraModule.Scripts.Shake {
    public interface ICameraShakeSampler {
        public void Advance(CameraShakeModel cameraShakeModel, float deltaTime);

        public CameraShakePose Sample(CameraShakeModel cameraShakeModel);
    }
}
