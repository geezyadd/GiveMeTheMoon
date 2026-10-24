using UnityEngine;

namespace Features.ShipModule.Scripts.Repair {
    // Marks a grabbable item as a repair tool of its definition.
    public sealed class RepairTool : MonoBehaviour {
        [SerializeField] private RepairToolDefinition _definition;

        public RepairToolDefinition Definition => _definition;
    }
}
