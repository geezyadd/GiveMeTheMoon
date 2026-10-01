using System;

namespace Features.ShopModule.Scripts.Data {
    public sealed class ShopModel {
        public bool IsOpen { get; private set; }

        public event Action OnOpenChanged;

        public void Open() =>
            SetOpen(true);

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
