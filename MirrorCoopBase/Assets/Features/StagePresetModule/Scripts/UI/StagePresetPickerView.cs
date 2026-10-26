using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Features.StagePresetModule.Scripts.UI {
    public sealed class StagePresetPickerView : StagePresetPickerViewBase {
        [SerializeField] private GameObject _panel;
        [SerializeField] private RectTransform _optionsRoot;
        [Tooltip("Inactive template: every option is a copy of it.")]
        [SerializeField] private StagePresetOptionButton _optionTemplate;
        [SerializeField] private Color _selectedColor = Color.white;
        [SerializeField] private Color _idleColor = Color.gray;
        [SerializeField] private Text _description;

        private readonly List<StagePresetOptionButton> _options = new List<StagePresetOptionButton>();

        public override void SetOptions(IReadOnlyList<string> labels) {
            ClearOptions();
            for (int i = 0; i < labels.Count; i++)
                _options.Add(CreateOption(labels[i], i));
        }

        public override void SetSelected(int optionIndex, string description) {
            for (int i = 0; i < _options.Count; i++)
                _options[i].SetColor(i == optionIndex ? _selectedColor : _idleColor);

            _description.text = description;
        }

        public override void SetVisible(bool visible) =>
            _panel.SetActive(visible);

        public override void DisposeView() {
            ClearOptions();
            base.DisposeView();
        }

        private StagePresetOptionButton CreateOption(string label, int optionIndex) {
            StagePresetOptionButton option = Instantiate(_optionTemplate, _optionsRoot);
            option.gameObject.SetActive(true);
            option.Bind(label, optionIndex);
            option.OnClicked += OnOptionButtonClicked;
            return option;
        }

        private void ClearOptions() {
            for (int i = 0; i < _options.Count; i++) {
                _options[i].OnClicked -= OnOptionButtonClicked;
                Destroy(_options[i].gameObject);
            }

            _options.Clear();
        }

        private void OnOptionButtonClicked(int optionIndex) =>
            InvokeOptionClicked(optionIndex);
    }
}
