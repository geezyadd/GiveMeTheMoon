using System;
using Features.SettingsModule.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Features.SettingsModule.UI {
    public abstract class SettingToggleViewBase : SettingElementViewBase {
        public Action<bool> OnValueChanged;

        public abstract ToggleSettingDefinition ToggleDefinition { get; }

        public override SettingDefinition Definition => ToggleDefinition;

        public abstract void SetLabel(string label);
        public abstract void SetValue(bool value);
    }

    // An on / off row: label and toggle.
    public sealed class SettingToggleView : SettingToggleViewBase {
        [SerializeField] private ToggleSettingDefinition _definition;
        [SerializeField] private Text _label;
        [SerializeField] private Toggle _toggle;

        public override ToggleSettingDefinition ToggleDefinition => _definition;

        protected override void OnEnable() {
            base.OnEnable();
            _toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        protected override void OnDisable() {
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            base.OnDisable();
        }

        public override void SetLabel(string label) =>
            _label.text = label;

        public override void SetValue(bool value) =>
            _toggle.SetIsOnWithoutNotify(value);

        private void OnToggleChanged(bool value) =>
            OnValueChanged?.Invoke(value);
    }
}
