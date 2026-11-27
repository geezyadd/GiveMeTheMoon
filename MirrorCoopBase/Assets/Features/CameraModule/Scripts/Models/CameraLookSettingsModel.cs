namespace Features.CameraModule.Scripts.Models {
    // How the mouse turns the view: degrees per input unit and whether up / down is inverted. Set from the player's
    // settings (CameraLookSettingsApplier), read by CameraLookDriver.
    public sealed class CameraLookSettingsModel {
        public float LookSensitivity { get; set; }
        public bool InvertY { get; set; }
    }
}
