using System;
using UnityEngine;

namespace Features.GrabModule.Scripts {
    public sealed class HoveredInteractableModel {
        private InteractableBase _interactable;
        private bool _interactableReady;
        private Grabbable _grabbable;

        public event Action OnChanged;

        public void SetInteractable(InteractableBase interactable, bool isReady) {
            if (_interactable == interactable && _interactableReady == isReady)
                return;

            _interactable = interactable;
            _interactableReady = isReady;
            OnChanged?.Invoke();
        }

        public void SetGrabbable(Grabbable grabbable) {
            if (_grabbable == grabbable)
                return;

            _grabbable = grabbable;
            OnChanged?.Invoke();
        }

        public void Clear() {
            SetInteractable(null, false);
            SetGrabbable(null);
        }

        public bool TryGetCurrent(out Component source, out bool isReady) {
            source = null;
            isReady = false;
            if (_interactable != null) {
                source = _interactable;
                isReady = _interactableReady;
                return true;
            }

            if (_grabbable != null) {
                source = _grabbable;
                isReady = true;
                return true;
            }

            return false;
        }
    }
}
