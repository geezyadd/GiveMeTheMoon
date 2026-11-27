using Features.GrabModule.Scripts;
using UnityEngine;

namespace Features.ShipDamageModule.Scripts.Repair {
    // Marks a grabbable item as a repair tool of its definition; an inventory slot shows the definition's icon or name.
    public sealed class RepairTool : MonoBehaviour, IInventoryItemDisplay {
        [SerializeField] private RepairToolDefinition _definition;

        public RepairToolDefinition Definition => _definition;
        public Sprite InventoryIcon => _definition.Icon;
        public string InventoryName => _definition.DisplayName;
    }
}
