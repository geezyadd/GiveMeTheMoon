using System;
using System.Collections.Generic;
using Features.MvpModule;
using Features.SettingsModule.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Features.SettingsModule.UI {
    public abstract class SettingsWindowViewBase : ViewBehaviour {
        public Action<int> OnTabClicked;
        public Action OnApplyClicked;
        public Action OnCancelClicked;
        public Action OnResetTabClicked;
        public Action OnBackClicked;
        public Action OnUnsavedSaveClicked;
        public Action OnUnsavedDiscardClicked;
        public Action OnConfirmKeepClicked;
        public Action OnConfirmRevertClicked;

        public abstract int TabCount { get; }

        public abstract void SelectTab(int index);
        public abstract List<SettingDefinition> GetTabDefinitions(int index);
        public abstract void SetApplyInteractable(bool interactable);
        public abstract void ShowUnsavedPopup(bool shown);
        public abstract void ShowConfirmPopup(bool shown);
        public abstract void SetConfirmText(string text);
    }

    // The settings window's frame: tab buttons and their containers, Apply / Cancel / Reset tab / Back, the
    // "unapplied changes" popup and the "keep display settings" popup.
    public sealed class SettingsWindowView : SettingsWindowViewBase {
        [SerializeField] private List<SettingsTab> _tabs = new();
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _resetTabButton;
        [SerializeField] private Button _backButton;
        [Header("Unapplied changes popup")]
        [SerializeField] private GameObject _unsavedPopup;
        [SerializeField] private Button _unsavedSaveButton;
        [SerializeField] private Button _unsavedDiscardButton;
        [Header("Keep display settings popup")]
        [SerializeField] private GameObject _confirmPopup;
        [SerializeField] private Text _confirmText;
        [SerializeField] private Button _confirmKeepButton;
        [SerializeField] private Button _confirmRevertButton;

        private readonly List<UnityEngine.Events.UnityAction> _tabListeners = new();

        public override int TabCount => _tabs.Count;

        protected override void OnEnable() {
            base.OnEnable();
            for (int index = 0; index < _tabs.Count; index++) {
                int tabIndex = index;
                UnityEngine.Events.UnityAction listener = () => OnTabClicked?.Invoke(tabIndex);
                _tabListeners.Add(listener);
                _tabs[index].Button.onClick.AddListener(listener);
            }

            _applyButton.onClick.AddListener(OnApply);
            _cancelButton.onClick.AddListener(OnCancel);
            _resetTabButton.onClick.AddListener(OnResetTab);
            _backButton.onClick.AddListener(OnBack);
            _unsavedSaveButton.onClick.AddListener(OnUnsavedSave);
            _unsavedDiscardButton.onClick.AddListener(OnUnsavedDiscard);
            _confirmKeepButton.onClick.AddListener(OnConfirmKeep);
            _confirmRevertButton.onClick.AddListener(OnConfirmRevert);
        }

        protected override void OnDisable() {
            for (int index = 0; index < _tabListeners.Count; index++)
                _tabs[index].Button.onClick.RemoveListener(_tabListeners[index]);

            _tabListeners.Clear();
            _applyButton.onClick.RemoveListener(OnApply);
            _cancelButton.onClick.RemoveListener(OnCancel);
            _resetTabButton.onClick.RemoveListener(OnResetTab);
            _backButton.onClick.RemoveListener(OnBack);
            _unsavedSaveButton.onClick.RemoveListener(OnUnsavedSave);
            _unsavedDiscardButton.onClick.RemoveListener(OnUnsavedDiscard);
            _confirmKeepButton.onClick.RemoveListener(OnConfirmKeep);
            _confirmRevertButton.onClick.RemoveListener(OnConfirmRevert);
            base.OnDisable();
        }

        // The chosen tab's button is not clickable, which also marks it as the open one.
        public override void SelectTab(int index) {
            for (int tabIndex = 0; tabIndex < _tabs.Count; tabIndex++) {
                bool isSelected = tabIndex == index;
                _tabs[tabIndex].Content.SetActive(isSelected);
                _tabs[tabIndex].Button.interactable = isSelected == false;
            }
        }

        public override List<SettingDefinition> GetTabDefinitions(int index) =>
            _tabs[index].GetDefinitions();

        public override void SetApplyInteractable(bool interactable) =>
            _applyButton.interactable = interactable;

        public override void ShowUnsavedPopup(bool shown) =>
            _unsavedPopup.SetActive(shown);

        public override void ShowConfirmPopup(bool shown) =>
            _confirmPopup.SetActive(shown);

        public override void SetConfirmText(string text) =>
            _confirmText.text = text;

        private void OnApply() =>
            OnApplyClicked?.Invoke();

        private void OnCancel() =>
            OnCancelClicked?.Invoke();

        private void OnResetTab() =>
            OnResetTabClicked?.Invoke();

        private void OnBack() =>
            OnBackClicked?.Invoke();

        private void OnUnsavedSave() =>
            OnUnsavedSaveClicked?.Invoke();

        private void OnUnsavedDiscard() =>
            OnUnsavedDiscardClicked?.Invoke();

        private void OnConfirmKeep() =>
            OnConfirmKeepClicked?.Invoke();

        private void OnConfirmRevert() =>
            OnConfirmRevertClicked?.Invoke();
    }
}
