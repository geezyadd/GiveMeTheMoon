using Features.GrabModule.Scripts;
using Features.ShipDamageModule.Scripts.Damage;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipDamageModule.Scripts.Repair {
    // A socket's broken module as a repair place, for a module repair tool. Slot views have no colliders and some
    // socket plates sit in the deck, so its repair volume is a trigger over the socket.
    public sealed class ShipModuleRepairInteractable : ShipRepairInteractableBase {
        [SerializeField] private ShipSocket _socket;

        private IShipModuleCondition _shipModuleCondition;
        private IShipConditionService _shipConditionService;

        public override bool NeedsRepair => _socket.IsOccupied && _shipModuleCondition.IsWorking(_socket.SocketId) == false;
        protected override ShipRepairKind RepairKind => ShipRepairKind.Module;

        [Inject]
        private void InjectDependencies(IShipModuleCondition shipModuleCondition, IShipConditionService shipConditionService) {
            _shipModuleCondition = shipModuleCondition;
            _shipConditionService = shipConditionService;
        }

        private void OnEnable() {
            _shipModuleCondition.OnConditionChanged += RefreshRepairVolume;
            RefreshRepairVolume();
        }

        private void OnDisable() =>
            _shipModuleCondition.OnConditionChanged -= RefreshRepairVolume;

        public override void ServerUse(NetworkIdentity user, GrabController grab) =>
            _shipConditionService.ServerRepairModule(_socket.SocketId);
    }
}
