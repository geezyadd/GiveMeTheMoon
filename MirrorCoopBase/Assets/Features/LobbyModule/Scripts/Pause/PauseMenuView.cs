using System;
using Features.MvpModule;
using UnityEngine;
using UnityEngine.UI;

namespace Features.LobbyModule.Scripts.Pause {
    public abstract class PauseMenuViewBase : ViewBehaviour {
        public Action OnContinueClicked;
        public Action OnSettingsClicked;
    }

    // The pause menu's Continue and Settings buttons (Leave is the LeaveSessionView next to them).
    public sealed class PauseMenuView : PauseMenuViewBase {
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingsButton;

        protected override void OnEnable() {
            base.OnEnable();
            _continueButton.onClick.AddListener(OnContinue);
            _settingsButton.onClick.AddListener(OnSettings);
        }

        protected override void OnDisable() {
            _continueButton.onClick.RemoveListener(OnContinue);
            _settingsButton.onClick.RemoveListener(OnSettings);
            base.OnDisable();
        }

        private void OnContinue() =>
            OnContinueClicked?.Invoke();

        private void OnSettings() =>
            OnSettingsClicked?.Invoke();
    }
}
