using UnityEngine;
using UnityEngine.UI;

namespace Features.StagePresetModule.Scripts.UI {
    public sealed class StagePresetLabelView : StagePresetLabelViewBase {
        [SerializeField] private Text _label;

        public override void ShowPreset(string text) {
            _label.text = text;
            _label.gameObject.SetActive(true);
        }

        public override void HidePreset() =>
            _label.gameObject.SetActive(false);
    }
}
