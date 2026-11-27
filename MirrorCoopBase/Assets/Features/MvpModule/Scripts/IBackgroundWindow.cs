namespace Features.MvpModule {
    // A window that stays open under the others for a whole game state (the HUD, the debug overlay). Systems that ask
    // "is a window in the way?" (Esc opening the pause menu) do not count it.
    public interface IBackgroundWindow : IWindow { }
}
