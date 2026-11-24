namespace Features.CameraModule.Scripts.Models {
    // How far the local player's carrier (a moving platform, the deck) turned since the camera last looked, degrees
    // around the up axis. The camera adds it to the look, so the view turns with what the player stands on. Local.
    public sealed class CameraCarryTurnModel {
        private float _pendingYaw;

        public void AddTurn(float yawDegrees) =>
            _pendingYaw += yawDegrees;

        public float TakeTurn() {
            float yaw = _pendingYaw;
            _pendingYaw = 0f;
            return yaw;
        }
    }
}
