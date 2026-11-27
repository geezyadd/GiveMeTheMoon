using System;
using System.Collections.Generic;
using Features.SettingsModule.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Features.SettingsModule.UI {
    // One tab of the settings window: its button and the container its setting rows are dropped into.
    [Serializable]
    public sealed class SettingsTab {
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _content;

        public Button Button => _button;
        public GameObject Content => _content;

        public List<SettingDefinition> GetDefinitions() {
            List<SettingDefinition> definitions = new();
            foreach (SettingElementViewBase element in _content.GetComponentsInChildren<SettingElementViewBase>(true))
                if (element.Definition != null)
                    definitions.Add(element.Definition);

            return definitions;
        }
    }
}
