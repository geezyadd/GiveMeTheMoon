using System;

namespace Features.PlayerLifeModule.Scripts.Spectator {
    public sealed class SpectatorModel {
        public bool IsSpectating { get; private set; }
        public string Label { get; private set; } = string.Empty;

        public event Action OnChanged;

        public void Show(string label) {
            if (IsSpectating && Label == label)
                return;

            IsSpectating = true;
            Label = label;
            OnChanged?.Invoke();
        }

        public void Hide() {
            if (IsSpectating == false)
                return;

            IsSpectating = false;
            Label = string.Empty;
            OnChanged?.Invoke();
        }

        public void Clear() {
            IsSpectating = false;
            Label = string.Empty;
        }
    }
}
