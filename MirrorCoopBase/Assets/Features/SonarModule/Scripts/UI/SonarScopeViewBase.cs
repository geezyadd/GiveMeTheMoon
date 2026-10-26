using System;
using System.Collections.Generic;
using Features.MvpModule;
using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using UnityEngine;

namespace Features.SonarModule.Scripts.UI {
    public abstract class SonarScopeViewBase : ViewBehaviour {
        public event Action OnPingClicked;
        public event Action OnCloseClicked;
        public event Action<string> OnBlipClicked;

        public abstract void SetRings(int ringCount);
        public abstract void ShowRingLabel(int ringIndex, string text);
        public abstract void ShowLegend(IReadOnlyList<SonarContactKind> kinds);
        public abstract void ShowHint(string text);
        public abstract void ShowBlips(IReadOnlyList<SonarBlip> blips);
        // Below 0: no wave.
        public abstract void ShowWave(float share);
        public abstract void ShowCooldown(float share);
        public abstract void ShowStatus(string text);
        public abstract void ShowNoise(bool isShown);
        public abstract void ShowNorth(Vector2 scopePosition);
        public abstract void ShowHeading(string text);
        public abstract void ShowStationBearing(bool isShown, Vector2 scopePosition);
        public abstract void ShowStationDistance(string text);
        public abstract void PlayPing();

        protected void InvokePingClicked() =>
            OnPingClicked?.Invoke();

        protected void InvokeCloseClicked() =>
            OnCloseClicked?.Invoke();

        protected void InvokeBlipClicked(string contactId) =>
            OnBlipClicked?.Invoke(contactId);
    }
}
