using System;
using System.Collections.Generic;
using Features.MvpModule;
using Features.SonarModule.Data;

namespace Features.SonarModule.Scripts.UI {
    public abstract class SonarScopeViewBase : ViewBehaviour {
        public event Action OnPingClicked;
        public event Action OnCloseClicked;
        public event Action<string> OnBlipClicked;

        public abstract void SetRings(int ringCount);
        public abstract void ShowBlips(IReadOnlyList<SonarBlip> blips);
        // Below 0: no wave.
        public abstract void ShowWave(float share);
        public abstract void ShowCooldown(float share);
        public abstract void ShowStatus(string text);
        public abstract void ShowNoise(bool isShown);
        public abstract void PlayPing();

        protected void InvokePingClicked() =>
            OnPingClicked?.Invoke();

        protected void InvokeCloseClicked() =>
            OnCloseClicked?.Invoke();

        protected void InvokeBlipClicked(string contactId) =>
            OnBlipClicked?.Invoke(contactId);
    }
}
