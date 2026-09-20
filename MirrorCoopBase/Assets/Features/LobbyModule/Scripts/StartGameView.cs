using System;
using Features.MvpModule;
using UnityEngine;
using UnityEngine.UI;

namespace Features.LobbyModule.Scripts {
    public abstract class StartGameViewBase : ViewBehaviour {
        public Action OnStartClicked;

        public abstract void SetInteractable(bool interactable);
        public abstract void SetVisible(bool visible);
    }

    public sealed class StartGameView : StartGameViewBase {
        [SerializeField] Button _button;

        protected override void OnEnable() {
            base.OnEnable();
            if (_button != null)
                _button.onClick.AddListener(OnClicked);
        }

        protected override void OnDisable() {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
            base.OnDisable();
        }

        public override void SetInteractable(bool interactable) {
            if (_button != null)
                _button.interactable = interactable;
        }

        public override void SetVisible(bool visible) {
            if (_button != null)
                _button.gameObject.SetActive(visible);
        }

        void OnClicked() =>
            OnStartClicked?.Invoke();
    }
}
