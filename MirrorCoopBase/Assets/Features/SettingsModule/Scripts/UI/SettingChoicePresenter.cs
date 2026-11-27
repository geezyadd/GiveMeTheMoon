using System.Collections.Generic;
using Features.MvpModule;
using Features.SettingsModule.Data;

namespace Features.SettingsModule.UI {
    // Shows a choice setting's draft option and steps the draft through the options, wrapping around.
    public sealed class SettingChoicePresenter : PresenterBehaviour<SettingChoiceViewBase> {
        private readonly SettingsDraftModel _draft;

        private IReadOnlyList<SettingChoiceOption> _options;

        public SettingChoicePresenter(SettingsDraftModel draft) =>
            _draft = draft;

        private ChoiceSettingDefinitionBase Definition => View.ChoiceDefinition;

        protected override void OnViewSet() {
            View.SetLabel(Definition.Label);
            _options = Definition.GetOptions();
            View.OnPreviousClicked += OnPreviousClicked;
            View.OnNextClicked += OnNextClicked;
            _draft.Values.OnValueChanged += OnDraftValueChanged;
            Refresh();
        }

        protected override void OnDisposed() {
            View.OnPreviousClicked -= OnPreviousClicked;
            View.OnNextClicked -= OnNextClicked;
            _draft.Values.OnValueChanged -= OnDraftValueChanged;
        }

        private void OnPreviousClicked() =>
            Step(-1);

        private void OnNextClicked() =>
            Step(1);

        private void Step(int direction) {
            if (_options.Count == 0)
                return;

            int index = FindShownIndex();
            int next = index < 0 ? 0 : ((index + direction) % _options.Count + _options.Count) % _options.Count;
            _draft.Values.SetChoice(Definition.Key, _options[next].Id);
        }

        private void OnDraftValueChanged(string key) {
            if (key == Definition.Key)
                Refresh();
        }

        private void Refresh() {
            int index = FindShownIndex();
            View.SetValueText(index >= 0 ? _options[index].Label : _draft.Values.GetChoice(Definition.Key));
        }

        private int FindShownIndex() {
            string shown = Definition.ResolveShownOptionId(_draft.Values.GetChoice(Definition.Key));
            for (int index = 0; index < _options.Count; index++)
                if (_options[index].Id == shown)
                    return index;

            return -1;
        }
    }
}
