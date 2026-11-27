using System;
using Features.SettingsModule.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Features.SettingsModule.UI {
    public abstract class SettingChoiceViewBase : SettingElementViewBase {
        public Action OnPreviousClicked;
        public Action OnNextClicked;

        public abstract ChoiceSettingDefinitionBase ChoiceDefinition { get; }

        public override SettingDefinition Definition => ChoiceDefinition;

        public abstract void SetLabel(string label);
        public abstract void SetValueText(string valueText);
    }

    // A choice row: label and "< value >".
    public sealed class SettingChoiceView : SettingChoiceViewBase {
        [SerializeField] private ChoiceSettingDefinitionBase _definition;
        [SerializeField] private Text _label;
        [SerializeField] private Text _valueText;
        [SerializeField] private Button _previousButton;
        [SerializeField] private Button _nextButton;

        public override ChoiceSettingDefinitionBase ChoiceDefinition => _definition;

        protected override void OnEnable() {
            base.OnEnable();
            _previousButton.onClick.AddListener(OnPrevious);
            _nextButton.onClick.AddListener(OnNext);
        }

        protected override void OnDisable() {
            _previousButton.onClick.RemoveListener(OnPrevious);
            _nextButton.onClick.RemoveListener(OnNext);
            base.OnDisable();
        }

        public override void SetLabel(string label) =>
            _label.text = label;

        public override void SetValueText(string valueText) =>
            _valueText.text = valueText;

        private void OnPrevious() =>
            OnPreviousClicked?.Invoke();

        private void OnNext() =>
            OnNextClicked?.Invoke();
    }
}
