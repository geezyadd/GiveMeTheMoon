using UnityEngine;
using UnityEngine.UI;

namespace Features.InventoryModule.Scripts.UI {
    // One slot of the inventory bar: a rounded frame on a dark background, the key number in a corner, and the item's
    // icon or, without one, its name. The selected slot grows and its frame brightens; an arriving item fades in.
    public sealed class InventorySlotView : MonoBehaviour {
        private const float IDLE_SCALE = 1f;
        private const float SHOWN_ALPHA = 1f;

        [SerializeField] private RectTransform _scaledRoot;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _keyLabel;
        [SerializeField] private Color _idleFrameColor = new(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color _selectedFrameColor = new(1f, 0.85f, 0.4f, 1f);

        private float _selectedScale = IDLE_SCALE;
        private float _animationSpeed;
        private float _iconFadeSpeed;
        private float _stowedIconAlpha;
        private float _selection;
        private float _targetSelection;
        private float _contentAlpha = SHOWN_ALPHA;
        private float _targetContentAlpha = SHOWN_ALPHA;
        private bool _hasItem;

        public void Setup(int keyNumber, InventoryConfiguration inventoryConfiguration) {
            _keyLabel.text = keyNumber.ToString();
            _selectedScale = inventoryConfiguration.SelectedSlotScale;
            _animationSpeed = inventoryConfiguration.SlotAnimationSpeed;
            _iconFadeSpeed = inventoryConfiguration.IconFadeSpeed;
            _stowedIconAlpha = inventoryConfiguration.StowedIconAlpha;
            ClearItem();
            ApplyLook();
        }

        public void SetItem(Sprite icon, string itemName) {
            if (_hasItem == false)
                _contentAlpha = 0f;

            _hasItem = true;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _nameLabel.text = itemName;
            _nameLabel.enabled = icon == null;
            ApplyLook();
        }

        public void ClearItem() {
            _hasItem = false;
            _icon.sprite = null;
            _icon.enabled = false;
            _nameLabel.text = string.Empty;
            _nameLabel.enabled = false;
        }

        public void SetSelected(bool isSelected, bool isStowed) {
            _targetSelection = isSelected ? 1f : 0f;
            _targetContentAlpha = isSelected && isStowed ? _stowedIconAlpha : SHOWN_ALPHA;
        }

        // UI time: the bar keeps animating while the game is paused.
        private void Update() {
            float deltaTime = Time.unscaledDeltaTime;
            _selection = Mathf.Lerp(_selection, _targetSelection, Blend(_animationSpeed, deltaTime));
            _contentAlpha = Mathf.Lerp(_contentAlpha, _targetContentAlpha, Blend(_iconFadeSpeed, deltaTime));
            ApplyLook();
        }

        private void ApplyLook() {
            _scaledRoot.localScale = Vector3.one * Mathf.Lerp(IDLE_SCALE, _selectedScale, _selection);
            _frame.color = Color.Lerp(_idleFrameColor, _selectedFrameColor, _selection);
            SetAlpha(_icon, _contentAlpha);
            SetAlpha(_nameLabel, _contentAlpha);
        }

        private static float Blend(float speed, float deltaTime) =>
            1f - Mathf.Exp(-speed * deltaTime);

        private static void SetAlpha(Graphic graphic, float alpha) {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
