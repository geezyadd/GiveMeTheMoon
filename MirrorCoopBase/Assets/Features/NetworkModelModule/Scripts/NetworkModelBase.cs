using System;

namespace Features.NetworkModelModule.Scripts {
    public abstract class NetworkModelBase {
        private object _feedingBridge;

        public bool IsAvailable { get; private set; }

        public event Action OnAvailableChanged;
        public event Action OnChanged;

        // While the map changes additively a shared model can have a bridge in the old and in the new scene for a
        // moment: the bridge that started last feeds the model, and only that bridge takes it down again.
        internal void AttachBridge(object bridge) {
            _feedingBridge = bridge;
            SetAvailable(true);
        }

        internal bool TryDetachBridge(object bridge) {
            if (_feedingBridge != bridge)
                return false;

            _feedingBridge = null;
            SetAvailable(false);
            return true;
        }

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
