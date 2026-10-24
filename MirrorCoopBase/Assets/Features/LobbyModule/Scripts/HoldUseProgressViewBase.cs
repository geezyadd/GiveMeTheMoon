using Features.MvpModule;

namespace Features.LobbyModule.Scripts {
    public abstract class HoldUseProgressViewBase : ViewBehaviour {
        public abstract void SetProgressVisible(bool isVisible);
        public abstract void SetProgress(float fraction);
    }
}
