using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts.Damage;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Repair {
    // A repair place: used by holding interact with a tool of its repair kind in the hand, while it needs repair.
    // Its repair volume is the collider the aim finds it by, on only while it needs repair.
    public abstract class ShipRepairInteractableBase : InteractableBase {
        [SerializeField] private Collider _repairVolume;

        private readonly HeldRepairTool _heldRepairTool = new HeldRepairTool();

        private ShipDamageConfiguration _shipDamageConfiguration;

        public abstract bool NeedsRepair { get; }
        protected abstract ShipRepairKind RepairKind { get; }

        [Inject]
        private void InjectDependencies(ShipDamageConfiguration shipDamageConfiguration) =>
            _shipDamageConfiguration = shipDamageConfiguration;

        public override bool CanUse(NetworkIdentity user, GrabController grab) =>
            NeedsRepair && _heldRepairTool.TryGet(grab, RepairKind, out _);

        public override bool TryGetHoldSeconds(GrabController grab, out float seconds) {
            seconds = 0f;
            if (_heldRepairTool.TryGet(grab, RepairKind, out RepairToolDefinition tool) == false)
                return false;

            seconds = _shipDamageConfiguration.RepairSeconds / tool.SpeedMultiplier;
            return true;
        }

        protected void RefreshRepairVolume() =>
            _repairVolume.enabled = NeedsRepair;
    }
}
