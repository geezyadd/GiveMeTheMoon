using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts.Damage;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Repair {
    // A socket's broken module as a repair place: usable with a module repair tool in the hand, by holding interact.
    // Slot views have no colliders and some socket plates sit in the deck, so the aim finds a broken module by its
    // repair volume over the socket, which is on only while the module is broken.
    public sealed class ShipModuleRepairInteractable : InteractableBase {
        [SerializeField] private ShipSocket _socket;
        [SerializeField] private Collider _repairVolume;

        private readonly HeldRepairTool _heldRepairTool = new HeldRepairTool();

        private IShipModuleCondition _shipModuleCondition;
        private IShipConditionService _shipConditionService;
        private ShipDamageConfiguration _shipDamageConfiguration;

        public bool NeedsRepair => _socket.IsOccupied && _shipModuleCondition.IsWorking(_socket.SocketId) == false;

        [Inject]
        private void InjectDependencies(
            IShipModuleCondition shipModuleCondition,
            IShipConditionService shipConditionService,
            ShipDamageConfiguration shipDamageConfiguration) {
            _shipModuleCondition = shipModuleCondition;
            _shipConditionService = shipConditionService;
            _shipDamageConfiguration = shipDamageConfiguration;
        }

        private void OnEnable() {
            _shipModuleCondition.OnConditionChanged += OnConditionChanged;
            OnConditionChanged();
        }

        private void OnDisable() =>
            _shipModuleCondition.OnConditionChanged -= OnConditionChanged;

        public override bool CanUse(NetworkIdentity user, GrabController grab) =>
            NeedsRepair && _heldRepairTool.TryGet(grab, ShipRepairKind.Module, out _);

        public override bool TryGetHoldSeconds(GrabController grab, out float seconds) {
            seconds = 0f;
            if (_heldRepairTool.TryGet(grab, ShipRepairKind.Module, out RepairToolDefinition tool) == false)
                return false;

            seconds = _shipDamageConfiguration.RepairSeconds / tool.SpeedMultiplier;
            return true;
        }

        public override void ServerUse(NetworkIdentity user, GrabController grab) =>
            _shipConditionService.ServerRepairModule(_socket.SocketId);

        private void OnConditionChanged() =>
            _repairVolume.enabled = NeedsRepair;
    }
}
