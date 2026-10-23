using System;
using UnityEngine;
using UnityEngine.UI;

namespace Features.StagePresetModule.Scripts.UI {
    // One option of the preset picker: a button that reports its option index.
    public sealed class StagePresetOptionButton : MonoBehaviour {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Text _label;

        private int _optionIndex;

        public event Action<int> OnClicked;

        private void OnEnable() =>
            _button.onClick.AddListener(OnButtonClicked);

        private void OnDisable() =>
            _button.onClick.RemoveListener(OnButtonClicked);

        public void Bind(string label, int optionIndex) {
            _label.text = label;
            _optionIndex = optionIndex;
        }

        public void SetColor(Color color) =>
            _background.color = color;

        private void OnButtonClicked() =>
            OnClicked?.Invoke(_optionIndex);
    }
}
