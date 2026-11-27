using System.Globalization;
using Features.MvpModule;
using Features.SettingsModule.Data;

namespace Features.SettingsModule.UI {
    // Shows a slider setting's draft value with its text and writes the player's change into the draft (the draft
    // snaps it to the slider's step, and the slider shows the snapped value).
    public sealed class SettingSliderPresenter : PresenterBehaviour<SettingSliderViewBase> {
        private readonly SettingsDraftModel _draft;

        public SettingSliderPresenter(SettingsDraftModel draft) =>
            _draft = draft;

        private SliderSettingDefinition Definition => View.SliderDefinition;

        protected override void OnViewSet() {
            View.SetLabel(Definition.Label);
            View.SetRange(Definition.Minimum, Definition.Maximum);
            View.OnValueChanged += OnValueChanged;
            _draft.Values.OnValueChanged += OnDraftValueChanged;
            Refresh();
        }

        protected override void OnDisposed() {
            View.OnValueChanged -= OnValueChanged;
            _draft.Values.OnValueChanged -= OnDraftValueChanged;
        }

        private void OnValueChanged(float value) {
            _draft.Values.SetSlider(Definition.Key, value);
            // The draft keeps its value when the slider moved less than a step; put the slider back on it.
            Refresh();
        }

        private void OnDraftValueChanged(string key) {
            if (key == Definition.Key)
                Refresh();
        }

        private void Refresh() {
            float value = _draft.Values.GetSlider(Definition.Key);
            View.SetValue(value, string.Format(CultureInfo.InvariantCulture, Definition.ValueFormat, value));
        }
    }
}
