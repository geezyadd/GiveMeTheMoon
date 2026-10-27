using Features.MvpModule;

namespace Features.LobbyModule.Scripts {
    public abstract class HeldWeaponReadinessViewBase : ViewBehaviour {
        public abstract void SetReadinessVisible(bool isVisible);
        public abstract void SetReadiness(float readiness);
    }
}
