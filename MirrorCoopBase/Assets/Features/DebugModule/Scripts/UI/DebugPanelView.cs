using System.Collections.Generic;
using Features.DebugModule.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Features.DebugModule.Scripts.UI {
    // The panel over the corner button: a scroll list of section titles and action rows built from templates, and a
    // hint line for the hovered action.
    public sealed class DebugPanelView : DebugPanelViewBase {
        private const string IDLE_HINT = "Hover an action to see what it does. Tab frees the cursor, Esc closes the panel.";

        [SerializeField] private GameObject _panel;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Text _sectionTemplate;
        [SerializeField] private DebugActionRowItem _rowTemplate;
        [SerializeField] private Text _hint;

        private readonly Dictionary<DebugActionDefinition, DebugActionRowItem> _rows = new();
        private readonly List<GameObject> _items = new();

        // Test hook: the loop test finds an action's button to click it.
        public bool TryGetDebugRow(DebugActionDefinition action, out DebugActionRowItem row) =>
            _rows.TryGetValue(action, out row);

        public override void DisposeView() {
            ClearActions();
            base.DisposeView();
        }

        public override void ShowPanel(bool isShown) {
            _panel.SetActive(isShown);
            _hint.text = IDLE_HINT;
        }

        public override void ClearActions() {
            foreach (DebugActionRowItem row in _rows.Values)
                UnsubscribeRow(row);

            foreach (GameObject item in _items)
                Destroy(item);

            _rows.Clear();
            _items.Clear();
            _sectionTemplate.gameObject.SetActive(false);
            _rowTemplate.gameObject.SetActive(false);
        }

        public override void AddSection(string title) {
            Text section = Instantiate(_sectionTemplate, _content);
            section.gameObject.SetActive(true);
            section.text = title;
            _items.Add(section.gameObject);
        }

        public override void AddAction(DebugActionDefinition action) {
            DebugActionRowItem row = Instantiate(_rowTemplate, _content);
            row.Show(action);
            row.OnRunClicked += OnRowRunClicked;
            row.OnHoverStarted += OnRowHoverStarted;
            row.OnHoverEnded += OnRowHoverEnded;
            _rows.Add(action, row);
            _items.Add(row.gameObject);
        }

        public override void ShowAvailability(DebugActionDefinition action, string unavailableReason) =>
            _rows[action].ShowAvailability(unavailableReason);

        private void UnsubscribeRow(DebugActionRowItem row) {
            row.OnRunClicked -= OnRowRunClicked;
            row.OnHoverStarted -= OnRowHoverStarted;
            row.OnHoverEnded -= OnRowHoverEnded;
        }

        private void OnRowRunClicked(DebugActionDefinition action, float value) =>
            InvokeActionClicked(action, value);

        private void OnRowHoverStarted(DebugActionRowItem row) =>
            _hint.text = row.Hint;

        private void OnRowHoverEnded(DebugActionRowItem row) =>
            _hint.text = IDLE_HINT;
    }
}
