using Features.EmoteModule.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Features.EmoteModule.UI {
    // One sector of the emote wheel: its ring piece, and the emote's icon (or its name, when it has no icon) in the
    // sector's middle.
    public sealed class EmoteWheelSector : MonoBehaviour {
        [SerializeField] private EmoteSectorGraphic _graphic;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Text _label;
        [SerializeField] private Image _icon;
        [Tooltip("How far the icon or the name stands from the wheel's centre, pixels.")]
        [SerializeField] private float _contentRadius = 150f;
        [SerializeField] private Color _normalColor = new(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color _highlightedColor = new(1f, 0.8f, 0.3f, 0.85f);

        public void Bind(int index, int count, EmoteEntry emote) {
            float sweep = 360f / count;
            _graphic.SetArc(EmoteWheelRule.SectorCenterDegrees(index, count) - sweep * 0.5f, sweep);
            _content.anchoredPosition = EmoteWheelRule.SectorDirection(index, count) * _contentRadius;
            _icon.sprite = emote.Icon;
            _icon.enabled = emote.Icon != null;
            _label.text = emote.DisplayName;
            _label.enabled = emote.Icon == null;
            SetHighlighted(false);
        }

        public void SetHighlighted(bool isHighlighted) =>
            _graphic.color = isHighlighted ? _highlightedColor : _normalColor;
    }
}
