using Features.MvpModule;

namespace Features.PlayerLifeModule.Scripts.Spectator {
    public abstract class SpectatorHudViewBase : ViewBehaviour {
        public abstract void SetLabel(string value);
    }
}
