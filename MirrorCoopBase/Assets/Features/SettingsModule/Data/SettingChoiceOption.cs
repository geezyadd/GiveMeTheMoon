using System;
using UnityEngine;

namespace Features.SettingsModule.Data {
    // One option of a choice setting: the id that is stored and the label the player sees.
    [Serializable]
    public sealed class SettingChoiceOption {
        [SerializeField] private string _id;
        [SerializeField] private string _label;

        public SettingChoiceOption(string id, string label) {
            _id = id;
            _label = label;
        }

        public string Id => _id;
        public string Label => _label;
    }
}
