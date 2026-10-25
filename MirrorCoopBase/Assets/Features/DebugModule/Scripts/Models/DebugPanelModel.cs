using System;

namespace Features.DebugModule.Scripts.Models {
    // Whether the local player has the debug window's panel open (the corner button stays either way).
    public sealed class DebugPanelModel {
        public bool IsOpen { get; private set; }

        public event Action OnOpenChanged;

        public void Toggle() =>
            SetOpen(IsOpen == false);

        public void Close() =>
            SetOpen(false);

        private void SetOpen(bool isOpen) {
            if (IsOpen == isOpen)
                return;

            IsOpen = isOpen;
            OnOpenChanged?.Invoke();
        }
    }
}
