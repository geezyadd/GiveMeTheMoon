using Features.MvpModule;

namespace Features.LobbyModule.Scripts {
    public abstract class ShipHullViewBase : ViewBehaviour {
        public abstract void SetHullVisible(bool isVisible);
        public abstract void SetHull(float fraction, string text);
        public abstract void PlayDamageFlash(float seconds, float alpha);
    }
}
