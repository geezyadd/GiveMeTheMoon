using UnityEngine;
using UnityEngine.UI;

namespace Features.DebugModule.Scripts.UI {
    // The small see-through button in the bottom-right corner: the only raycast target of the window while the
    // panel is closed, so it takes no clicks from the game around it.
    public sealed class DebugToggleView : DebugToggleViewBase {
        private const string CLOSED_LABEL = "DEBUG";
        private const string OPEN_LABEL = "CLOSE DEBUG";

        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _closedAlpha = 0.45f;

        // Test hook: the loop test clicks the corner button as a player does.
        public Button DebugButton => _button;

        protected override void OnEnable() {
            base.OnEnable();
            _button.onClick.AddListener(OnButtonClicked);
        }

        protected override void OnDisable() {
            _button.onClick.RemoveListener(OnButtonClicked);
            base.OnDisable();
        }

        public override void ShowPanelOpen(bool isOpen) {
            _label.text = isOpen ? OPEN_LABEL : CLOSED_LABEL;
            _canvasGroup.alpha = isOpen ? 1f : _closedAlpha;
        }

        private void OnButtonClicked() =>
            InvokeClicked();
    }
}
