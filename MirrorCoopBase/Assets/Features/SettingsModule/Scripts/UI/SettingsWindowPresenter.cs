using Features.MvpModule;
using Features.SettingsModule.Data;
using Features.SettingsModule.Saving;

namespace Features.SettingsModule.UI {
    // The settings window's flow: the draft starts as the live values; Apply writes it into the live model, Cancel
    // takes the live values back, Reset tab puts the open tab's defaults into the draft. Closing with unapplied
    // changes asks first; a window mode or resolution change must be kept within the configured seconds. The live
    // values are saved on Apply and again when a display change is kept or reverted.
    public sealed class SettingsWindowPresenter : PresenterBehaviour<SettingsWindowViewBase> {
        private readonly SettingsModel _liveModel;
        private readonly SettingsDraftModel _draft;
        private readonly SettingsWindowConfiguration _configuration;
        private readonly IWindowsService _windowsService;
        private readonly ISettingsSaving _settingsSaving;
        private readonly DisplaySettingsConfirmation _confirmation;

        private int _tabIndex;
        private bool _unsavedPopupShown;
        private bool _closeAfterConfirmation;

        public SettingsWindowPresenter(
            SettingsModel liveModel,
            SettingsDraftModel draft,
            SettingsWindowConfiguration configuration,
            IWindowsService windowsService,
            ISettingsSaving settingsSaving) {
            _liveModel = liveModel;
            _draft = draft;
            _configuration = configuration;
            _windowsService = windowsService;
            _settingsSaving = settingsSaving;
            _confirmation = new DisplaySettingsConfirmation(liveModel, draft.Values);
        }

        private bool HasUnappliedChanges => _draft.Values.HasSameValues(_liveModel) == false;

        protected override void OnViewSet() {
            _draft.Values.CopyValuesFrom(_liveModel);
            View.OnTabClicked += SelectTab;
            View.OnApplyClicked += Apply;
            View.OnCancelClicked += Cancel;
            View.OnResetTabClicked += ResetTab;
            View.OnBackClicked += RequestClose;
            View.OnUnsavedSaveClicked += SaveAndClose;
            View.OnUnsavedDiscardClicked += DiscardAndClose;
            View.OnConfirmKeepClicked += KeepDisplay;
            View.OnConfirmRevertClicked += RevertDisplay;
            _draft.Values.OnValueChanged += OnDraftValueChanged;
            View.ShowUnsavedPopup(false);
            View.ShowConfirmPopup(false);
            SelectTab(0);
            RefreshApply();
        }

        protected override void OnDisposed() {
            View.OnTabClicked -= SelectTab;
            View.OnApplyClicked -= Apply;
            View.OnCancelClicked -= Cancel;
            View.OnResetTabClicked -= ResetTab;
            View.OnBackClicked -= RequestClose;
            View.OnUnsavedSaveClicked -= SaveAndClose;
            View.OnUnsavedDiscardClicked -= DiscardAndClose;
            View.OnConfirmKeepClicked -= KeepDisplay;
            View.OnConfirmRevertClicked -= RevertDisplay;
            _draft.Values.OnValueChanged -= OnDraftValueChanged;
            // Closed from outside (a scene change) while the display change was not kept yet: it goes back.
            if (_confirmation.IsRunning) {
                _confirmation.Revert();
                _settingsSaving.Save();
            }
        }

        // Back and Esc: close an open question first, then ask about unapplied changes, then close.
        public void RequestClose() {
            if (_confirmation.IsRunning) {
                RevertDisplay();
                return;
            }

            // Esc on the question goes back to the window.
            if (_unsavedPopupShown) {
                HideUnsavedPopup();
                return;
            }

            if (HasUnappliedChanges) {
                _unsavedPopupShown = true;
                View.ShowUnsavedPopup(true);
                return;
            }

            Close();
        }

        private void SelectTab(int index) {
            if (index < 0 || index >= View.TabCount)
                return;

            _tabIndex = index;
            View.SelectTab(index);
        }

        private void Apply() {
            if (HasUnappliedChanges == false)
                return;

            bool displayChanged = DisplaySettingsConfirmation.DiffersInDisplay(_draft.Values, _liveModel);
            if (displayChanged)
                _confirmation.Remember();

            _liveModel.CopyValuesFrom(_draft.Values);
            _settingsSaving.Save();
            if (displayChanged)
                StartConfirmation();

            RefreshApply();
        }

        private void Cancel() =>
            _draft.Values.CopyValuesFrom(_liveModel);

        private void ResetTab() {
            foreach (SettingDefinition definition in View.GetTabDefinitions(_tabIndex))
                _draft.Values.ResetToDefault(definition.Key);
        }

        private void SaveAndClose() {
            HideUnsavedPopup();
            Apply();
            if (_confirmation.IsRunning)
                _closeAfterConfirmation = true;
            else
                Close();
        }

        private void DiscardAndClose() {
            HideUnsavedPopup();
            Cancel();
            Close();
        }

        private void StartConfirmation() {
            View.ShowConfirmPopup(true);
            _confirmation.Start(_configuration.DisplayConfirmSeconds, ShowSecondsLeft, OnConfirmationEnded);
        }

        private void ShowSecondsLeft(int seconds) {
            if (View != null && View.IsViewDisposed == false)
                View.SetConfirmText(string.Format(_configuration.DisplayConfirmFormat, seconds));
        }

        private void KeepDisplay() {
            _confirmation.Keep();
            OnConfirmationEnded();
        }

        private void RevertDisplay() {
            _confirmation.Revert();
            OnConfirmationEnded();
        }

        // After Keep the saved values are already right; after a revert the old display values are saved again.
        private void OnConfirmationEnded() {
            _settingsSaving.Save();
            if (View == null || View.IsViewDisposed)
                return;

            View.ShowConfirmPopup(false);
            RefreshApply();
            if (_closeAfterConfirmation) {
                _closeAfterConfirmation = false;
                Close();
            }
        }

        private void HideUnsavedPopup() {
            _unsavedPopupShown = false;
            View.ShowUnsavedPopup(false);
        }

        private void OnDraftValueChanged(string key) =>
            RefreshApply();

        private void RefreshApply() =>
            View.SetApplyInteractable(HasUnappliedChanges);

        private void Close() {
            IWindow window = _windowsService.GetWindow(typeof(SettingsWindow));
            if (window != null && window.WindowStatus != WindowStatus.Closed)
                _windowsService.CloseWindow<SettingsWindow>();
        }
    }
}
