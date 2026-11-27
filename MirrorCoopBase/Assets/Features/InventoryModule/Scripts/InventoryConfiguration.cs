using Features.InventoryModule.Scripts.UI;
using UnityEngine;

namespace Features.InventoryModule.Scripts {
    [CreateAssetMenu(
        fileName = nameof(InventoryConfiguration) + "_Default",
        menuName = "Configurations/InventoryModule/" + nameof(InventoryConfiguration))]
    public sealed class InventoryConfiguration : ScriptableObject {
        [Tooltip("How many slots a player has; the keys 1-4 select the first four, the mouse wheel cycles all.")]
        [SerializeField, Min(1)] private int _slotCount = 4;

        [Tooltip("One slot of the HUD's inventory bar.")]
        [SerializeField] private InventorySlotView _slotViewPrefab;

        [Tooltip("Seconds the release button is held before the item in the hand is thrown; a shorter press drops it.")]
        [SerializeField, Min(0f)] private float _throwHoldSeconds = 0.3f;

        [Tooltip("Speed, m/s, a thrown item leaves along the view, on top of the player's own velocity.")]
        [SerializeField, Min(0f)] private float _throwSpeed = 9f;

        [Tooltip("Radius, m, the carried items scatter in around the player when everything drops (death, leaving).")]
        [SerializeField, Min(0f)] private float _dropScatterRadius = 0.4f;

        [Tooltip("Scale of the selected slot in the HUD bar.")]
        [SerializeField, Min(1f)] private float _selectedSlotScale = 1.15f;

        [Tooltip("How fast, 1/s, a slot moves to its selected or idle look (20 = about 0.1 s).")]
        [SerializeField, Min(0f)] private float _slotAnimationSpeed = 20f;

        [Tooltip("How fast, 1/s, an item's icon fades in when it arrives in a slot.")]
        [SerializeField, Min(0f)] private float _iconFadeSpeed = 10f;

        [Tooltip("Opacity of the selected slot's icon while its item is stowed (the hand is empty).")]
        [SerializeField, Range(0f, 1f)] private float _stowedIconAlpha = 0.35f;

        public int SlotCount => _slotCount;
        public InventorySlotView SlotViewPrefab => _slotViewPrefab;
        public float ThrowHoldSeconds => _throwHoldSeconds;
        public float ThrowSpeed => _throwSpeed;
        public float DropScatterRadius => _dropScatterRadius;
        public float SelectedSlotScale => _selectedSlotScale;
        public float SlotAnimationSpeed => _slotAnimationSpeed;
        public float IconFadeSpeed => _iconFadeSpeed;
        public float StowedIconAlpha => _stowedIconAlpha;
    }
}
