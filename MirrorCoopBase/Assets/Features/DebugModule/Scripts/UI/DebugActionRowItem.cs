using System;
using System.Globalization;
using Features.DebugModule.Contracts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.DebugModule.Scripts.UI {
    // One action of the panel: its button, the number field (or the slider, for a value with a range) when the action
    // takes a value, and the reason it is greyed out. Hovering it shows its description and that reason in the panel's hint line.
    public sealed class DebugActionRowItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        private const string HINT_FORMAT = "{0}\n<color=#FF9A7A>{1}</color>";
        private const string SLIDER_LABEL_FORMAT = "{0} {1:0.##}";

        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [SerializeField] private GameObject _valueRoot;
        [SerializeField] private Text _valueLabel;
        [SerializeField] private InputField _valueField;
        [Tooltip("Shown instead of the number field for a value with a range; every move runs the action.")]
        [SerializeField] private Slider _valueSlider;
        [SerializeField] private Text _reason;

        private DebugActionDefinition _action;
        private string _unavailableReason;

        public DebugActionDefinition Action => _action;
        public string Hint => _unavailableReason == null ? _action.Description : string.Format(HINT_FORMAT, _action.Description, _unavailableReason);

        // Test hooks: the loop test types a value and clicks an action's button as a player does.
        public Button DebugButton => _button;
        public InputField DebugValueField => _valueField;

        public event Action<DebugActionDefinition, float> OnRunClicked;
        public event Action<DebugActionRowItem> OnHoverStarted;
        public event Action<DebugActionRowItem> OnHoverEnded;

        private void OnEnable() {
            _button.onClick.AddListener(OnButtonClicked);
            _valueSlider.onValueChanged.AddListener(OnSliderMoved);
        }

        private void OnDisable() {
            _button.onClick.RemoveListener(OnButtonClicked);
            _valueSlider.onValueChanged.RemoveListener(OnSliderMoved);
        }

        public void Show(DebugActionDefinition action) {
            _action = action;
            gameObject.SetActive(true);
            _label.text = action.Label;
            _valueRoot.SetActive(action.HasValue);
            if (action.HasValue == false)
                return;

            DebugActionValue value = action.Value;
            _valueLabel.text = value.Label;
            _valueField.gameObject.SetActive(value.HasRange == false);
            _valueSlider.gameObject.SetActive(value.HasRange);
            if (value.HasRange == false) {
                _valueField.text = value.DefaultValue.ToString(CultureInfo.InvariantCulture);
                return;
            }

            _valueSlider.minValue = value.Min;
            _valueSlider.maxValue = value.Max;
            _valueSlider.SetValueWithoutNotify(value.ShownValue);
            ShowSliderValue(value.ShownValue);
        }

        public void ShowAvailability(string unavailableReason) {
            _unavailableReason = unavailableReason;
            bool isAvailable = unavailableReason == null;
            _button.interactable = isAvailable;
            _reason.gameObject.SetActive(isAvailable == false);
            _reason.text = unavailableReason;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            OnHoverStarted?.Invoke(this);

        public void OnPointerExit(PointerEventData eventData) =>
            OnHoverEnded?.Invoke(this);

        // A slider runs its action at once on every move, while the action can run.
        private void OnSliderMoved(float value) {
            if (_action == null || _action.HasValue == false || _action.Value.HasRange == false)
                return;

            ShowSliderValue(value);
            if (_unavailableReason == null)
                OnRunClicked?.Invoke(_action, value);
        }

        private void ShowSliderValue(float value) =>
            _valueLabel.text = string.Format(CultureInfo.InvariantCulture, SLIDER_LABEL_FORMAT, _action.Value.Label, value);

        private float ReadValue() {
            if (_action.Value.HasRange)
                return _valueSlider.value;

            bool isParsed = float.TryParse(_valueField.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
            return isParsed ? value : _action.Value.DefaultValue;
        }

        private void OnButtonClicked() =>
            OnRunClicked?.Invoke(_action, _action.HasValue ? ReadValue() : 0f);
    }
}
