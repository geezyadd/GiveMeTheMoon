using System;
using Features.MvpModule;
using UnityEngine;
using UnityEngine.UI;

namespace Features.MenuModule.Scripts {
    public abstract class MenuSettingsViewBase : ViewBehaviour {
        public Action OnSettingsClicked;
    }

    // The main menu's Settings button.
    public sealed class MenuSettingsView : MenuSettingsViewBase {
        [SerializeField] private Button _button;

        protected override void OnEnable() {
            base.OnEnable();
            _button.onClick.AddListener(OnClicked);
        }

        protected override void OnDisable() {
            _button.onClick.RemoveListener(OnClicked);
            base.OnDisable();
        }

        private void OnClicked() =>
            OnSettingsClicked?.Invoke();
    }
}
