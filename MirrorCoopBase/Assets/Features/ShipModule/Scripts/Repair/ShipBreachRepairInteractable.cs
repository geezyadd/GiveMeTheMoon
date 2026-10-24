using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts.Damage;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Repair {
    // An open breach as a repair place: usable with a breach repair tool in the hand, by holding interact.
    public sealed class ShipBreachRepairInteractable : InteractableBase {
        [SerializeField] private ShipHullDamagePoint _point;
        [SerializeField] private Collider _collider;

        private readonly HeldRepairTool _heldRepairTool = new HeldRepairTool();

        private IShipBreachCondition _shipBreachCondition;
        private IShipConditionService _shipConditionService;
        private ShipDamageConfiguration _shipDamageConfiguration;

        [Inject]
        private void InjectDependencies(
            IShipBreachCondition shipBreachCondition,
            IShipConditionService shipConditionService,
            ShipDamageConfiguration shipDamageConfiguration) {
            _shipBreachCondition = shipBreachCondition;
            _shipConditionService = shipConditionService;
            _shipDamageConfiguration = shipDamageConfiguration;
        }

        // A closed point is not there for the aim at all: the collider is on only while the breach is open.
        private void OnEnable() {
            _shipBreachCondition.OnBreachesChanged += OnBreachesChanged;
            OnBreachesChanged();
        }

        private void OnDisable() =>
            _shipBreachCondition.OnBreachesChanged -= OnBreachesChanged;

        public override bool CanUse(NetworkIdentity user, GrabController grab) =>
            _point.IsOpen && _heldRepairTool.TryGet(grab, ShipRepairKind.Breach, out _);

        public override bool TryGetHoldSeconds(GrabController grab, out float seconds) {
            seconds = 0f;
            if (_heldRepairTool.TryGet(grab, ShipRepairKind.Breach, out RepairToolDefinition tool) == false)
                return false;

            seconds = _shipDamageConfiguration.RepairSeconds / tool.SpeedMultiplier;
            return true;
        }

        public override void ServerUse(NetworkIdentity user, GrabController grab) =>
            _shipConditionService.ServerRepairBreach(_point.PointId);

        private void OnBreachesChanged() =>
            _collider.enabled = _point.IsOpen;
    }
}
