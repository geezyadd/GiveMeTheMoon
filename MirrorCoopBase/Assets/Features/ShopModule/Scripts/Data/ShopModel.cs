using System;
using UnityEngine;

namespace Features.ShopModule.Scripts.Data {
    public sealed class ShopModel {
        public bool IsOpen { get; private set; }

        // The kiosk the window was opened at; null when the debug key opened it with no kiosk around.
        public Transform Kiosk { get; private set; }

        public event Action OnOpenChanged;

        public void Open(Transform kiosk) {
            Kiosk = kiosk;
            SetOpen(true);
        }

        public void Close() {
            Kiosk = null;
            SetOpen(false);
        }

        private void SetOpen(bool isOpen) {
            if (IsOpen == isOpen)
                return;

            IsOpen = isOpen;
            OnOpenChanged?.Invoke();
        }
    }
}
