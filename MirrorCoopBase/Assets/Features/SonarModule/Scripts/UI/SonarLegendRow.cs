using Features.SonarModule.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Features.SonarModule.Scripts.UI {
    // One line of the scope's legend: a kind's icon in its colour and its name.
    public sealed class SonarLegendRow : MonoBehaviour {
        [SerializeField] private Image _icon;
        [SerializeField] private Text _label;

        public void Show(SonarContactKind kind) {
            gameObject.SetActive(true);
            _icon.sprite = kind.Icon;
            Color color = kind.Color;
            color.a = 1f;
            _icon.color = color;
            _label.text = kind.DisplayName;
        }
    }
}
