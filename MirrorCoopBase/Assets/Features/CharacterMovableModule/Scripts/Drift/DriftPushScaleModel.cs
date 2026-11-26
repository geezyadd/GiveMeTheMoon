namespace Features.CharacterMovableModule.Scripts.Drift {
    // How much stronger than the settings this peer's own drifting body pushes with the move keys (acceleration and
    // top speed); 1 by default. Local, nothing synced: the owner pushes its own body. Set by a debug slider.
    public sealed class DriftPushScaleModel {
        public float Scale { get; set; } = 1f;
    }
}
