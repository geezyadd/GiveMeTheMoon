using System;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts.Damage;
using Features.ShipModule.Scripts.Repair;
using UnityEngine;

namespace Features.TooltipModule.Scripts {
    // A broken module tells why it cannot be used: any tooltip of its socket (install, remove, seat) shows the repair
    // tooltip's idle text instead. When the repair itself is the usable target its own tooltip is shown as usual.
    public sealed class ShipModuleRepairTooltipProvider : ITooltipStateProvider {
        private readonly IShipModuleCondition _shipModuleCondition;
        private readonly HoveredInteractableModel _hoveredInteractableModel;

        public ShipModuleRepairTooltipProvider(IShipModuleCondition shipModuleCondition, HoveredInteractableModel hoveredInteractableModel) {
            _shipModuleCondition = shipModuleCondition;
            _hoveredInteractableModel = hoveredInteractableModel;
        }

        public event Action OnStateChanged {
            add => _shipModuleCondition.OnConditionChanged += value;
            remove => _shipModuleCondition.OnConditionChanged -= value;
        }

        public bool TrySelect(InteractionTooltip tooltip, out TooltipData data, out string description) {
            data = null;
            description = null;
            ShipModuleRepairInteractable repair = tooltip.GetComponentInParent<ShipModuleRepairInteractable>();
            if (repair == null || repair.NeedsRepair == false || IsRepairHovered(repair))
                return false;

            data = InteractionTooltip.FindFor(repair).Select(false);
            description = data.Description;
            return true;
        }

        private bool IsRepairHovered(ShipModuleRepairInteractable repair) =>
            _hoveredInteractableModel.TryGetCurrent(out Component source, out _) && source == repair;
    }
}
