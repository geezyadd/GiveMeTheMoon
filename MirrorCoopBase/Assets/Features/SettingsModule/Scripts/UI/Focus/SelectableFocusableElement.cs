using System;
using Features.MvpModule;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.SettingsModule.UI.Focus {
    // A uGUI control as a focusable element of its window: keyboard / gamepad navigation reaches it only while its
    // window is the focused one (FocusablesService), and focusing it selects it in the UI event system, so the arrows,
    // the stick and Enter / the submit button work on it.
    public sealed class SelectableFocusableElement : MonoBehaviour, IFocusableElement {
        [SerializeField] private Selectable _selectable;
        [Tooltip("The element the window selects when it gets the focus.")]
        [SerializeField] private bool _isDefault;

        private Navigation _navigation;
        private bool _hasNavigation;

        public bool IsDefault => _isDefault;
        public bool IsFocused { get; private set; }
        public bool IsFocusable { get; private set; }

        public event Action OnFocused;
        public event Action OnUnFocused;

        public bool CanBeSelected =>
            _selectable != null && _selectable.isActiveAndEnabled && _selectable.IsInteractable();

        public void Focus(Action onInteract) {
            if (CanBeSelected == false || EventSystem.current == null)
                return;

            EventSystem.current.SetSelectedGameObject(_selectable.gameObject);
            IsFocused = true;
            OnFocused?.Invoke();
        }

        public void UnFocus() {
            if (IsFocused == false)
                return;

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _selectable.gameObject)
                EventSystem.current.SetSelectedGameObject(null);

            IsFocused = false;
            OnUnFocused?.Invoke();
        }

        public void MakeFocusable() {
            IsFocusable = true;
            if (_selectable != null && _hasNavigation)
                _selectable.navigation = _navigation;
        }

        // The control keeps working with the mouse; only navigation stops reaching it.
        public void MakeUnFocusable() {
            if (_selectable != null && _hasNavigation == false) {
                _navigation = _selectable.navigation;
                _hasNavigation = true;
            }

            UnFocus();
            IsFocusable = false;
            if (_selectable != null)
                _selectable.navigation = new Navigation { mode = Navigation.Mode.None };
        }
    }
}
