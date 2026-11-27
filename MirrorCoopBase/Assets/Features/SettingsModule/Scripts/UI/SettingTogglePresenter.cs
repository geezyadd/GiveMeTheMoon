using Features.MvpModule;

namespace Features.SettingsModule.UI {
    // Shows a toggle setting's draft value and writes the player's change into the draft.
    public sealed class SettingTogglePresenter : PresenterBehaviour<SettingToggleViewBase> {
        private readonly SettingsDraftModel _draft;

        public SettingTogglePresenter(SettingsDraftModel draft) =>
            _draft = draft;

        private string Key => View.Definition.Key;

        protected override void OnViewSet() {
            View.SetLabel(View.Definition.Label);
            View.OnValueChanged += OnValueChanged;
            _draft.Values.OnValueChanged += OnDraftValueChanged;
            Refresh();
        }

        protected override void OnDisposed() {
            View.OnValueChanged -= OnValueChanged;
            _draft.Values.OnValueChanged -= OnDraftValueChanged;
        }

        private void OnValueChanged(bool value) =>
            _draft.Values.SetToggle(Key, value);

        private void OnDraftValueChanged(string key) {
            if (key == Key)
                Refresh();
        }

        private void Refresh() =>
            View.SetValue(_draft.Values.GetToggle(Key));
    }
}
