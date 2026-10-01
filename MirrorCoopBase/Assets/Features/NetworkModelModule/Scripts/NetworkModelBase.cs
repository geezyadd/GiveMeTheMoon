using System;

namespace Features.NetworkModelModule.Scripts {
    public abstract class NetworkModelBase {
        public bool IsAvailable { get; private set; }

        public event Action OnAvailableChanged;
        public event Action OnChanged;

        internal void SetAvailable(bool value) {
            if (IsAvailable == value)
                return;

            IsAvailable = value;
            OnAvailableChanged?.Invoke();
            OnChanged?.Invoke();
        }

        protected void RaiseChanged() =>
            OnChanged?.Invoke();
    }
}
