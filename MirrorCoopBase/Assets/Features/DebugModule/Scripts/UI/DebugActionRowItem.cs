using System;
using System.Globalization;
using Features.DebugModule.Contracts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Features.DebugModule.Scripts.UI {
    // One action of the panel: its button, the number field when the action takes a value, and the reason it is
    // greyed out. Hovering it shows its description and that reason in the panel's hint line.
    public sealed class DebugActionRowItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        private const string HINT_FORMAT = "{0}\n<color=#FF9A7A>{1}</color>";

        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [SerializeField] private GameObject _valueRoot;
        [SerializeField] private Text _valueLabel;
        [SerializeField] private InputField _valueField;
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

        private void OnEnable() =>
            _button.onClick.AddListener(OnButtonClicked);

        private void OnDisable() =>
            _button.onClick.RemoveListener(OnButtonClicked);

        public void Show(DebugActionDefinition action) {
            _action = action;
            gameObject.SetActive(true);
            _label.text = action.Label;
            _valueRoot.SetActive(action.HasValue);
            if (action.HasValue == false)
                return;

            _valueLabel.text = action.Value.Label;
            _valueField.text = action.Value.DefaultValue.ToString(CultureInfo.InvariantCulture);
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

        private float ReadValue() {
            bool isParsed = float.TryParse(_valueField.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
            return isParsed ? value : _action.Value.DefaultValue;
        }

        private void OnButtonClicked() =>
            OnRunClicked?.Invoke(_action, _action.HasValue ? ReadValue() : 0f);
    }
}
