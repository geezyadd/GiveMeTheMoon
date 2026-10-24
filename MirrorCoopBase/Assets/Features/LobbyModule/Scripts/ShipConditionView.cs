using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Features.LobbyModule.Scripts {
    // The compact ship panel: hull text, a row of module tiles made from one template and the breach count.
    public sealed class ShipConditionView : ShipConditionViewBase {
        [SerializeField] private GameObject _content;
        [SerializeField] private Text _hullLabel;
        [SerializeField] private Text _breachLabel;
        [SerializeField] private Color _breachColor = new Color(1f, 0.45f, 0.2f, 1f);
        [SerializeField] private Color _noBreachColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Transform _moduleRoot;
        [SerializeField] private ShipConditionModuleIcon _moduleTemplate;

        private readonly List<ShipConditionModuleIcon> _moduleIcons = new List<ShipConditionModuleIcon>();

        public override void SetPanelVisible(bool isVisible) =>
            _content.SetActive(isVisible);

        public override void SetHull(string text) =>
            _hullLabel.text = text;

        public override void SetBreaches(string text, bool hasBreaches) {
            _breachLabel.text = text;
            _breachLabel.color = hasBreaches ? _breachColor : _noBreachColor;
        }

        public override void SetModules(IReadOnlyList<ShipConditionModuleDisplay> modules, float blinkSeconds, float blinksPerSecond) {
            while (_moduleIcons.Count < modules.Count)
                _moduleIcons.Add(Instantiate(_moduleTemplate, _moduleRoot));

            for (int i = 0; i < _moduleIcons.Count; i++) {
                if (i < modules.Count)
                    _moduleIcons[i].Show(modules[i], blinkSeconds, blinksPerSecond);
                else
                    _moduleIcons[i].Hide();
            }
        }
    }
}
