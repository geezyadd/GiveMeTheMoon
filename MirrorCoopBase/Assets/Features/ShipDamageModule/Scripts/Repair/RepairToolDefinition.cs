using Features.ShipDamageModule.Scripts.Damage;
using UnityEngine;

namespace Features.ShipDamageModule.Scripts.Repair {
    // The single description of one repair tool: what it repairs and how fast; code reads every tool only through it.
    [CreateAssetMenu(menuName = "Game/Repair Tool Definition", fileName = "RepairToolDefinition")]
    public sealed class RepairToolDefinition : ScriptableObject {
        [Tooltip("Stable id: never rename it once it is used.")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private long _price;
        [SerializeField] private RepairTool _itemPrefab;
        [Tooltip("What this tool repairs.")]
        [SerializeField] private ShipRepairKind _repairs;
        [Tooltip("Divides the configured repair time: 2 = twice as fast.")]
        [SerializeField, Min(0.1f)] private float _speedMultiplier = 1f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public long Price => _price;
        public RepairTool ItemPrefab => _itemPrefab;
        public ShipRepairKind Repairs => _repairs;
        public float SpeedMultiplier => _speedMultiplier;
    }
}
