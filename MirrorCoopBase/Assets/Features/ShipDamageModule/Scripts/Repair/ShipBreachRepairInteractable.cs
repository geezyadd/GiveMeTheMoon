using Features.GrabModule.Scripts;
using Features.ShipDamageModule.Scripts.Damage;
using Features.ShipModule.Scripts.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipDamageModule.Scripts.Repair {
    // An open breach as a repair place, for a breach repair tool; a closed point is not there for the aim at all.
    public sealed class ShipBreachRepairInteractable : ShipRepairInteractableBase {
        [SerializeField] private ShipHullDamagePoint _point;

        private IShipBreachCondition _shipBreachCondition;
        private IShipConditionService _shipConditionService;

        public override bool NeedsRepair => _point.IsOpen;
        protected override ShipRepairKind RepairKind => ShipRepairKind.Breach;

        [Inject]
        private void InjectDependencies(IShipBreachCondition shipBreachCondition, IShipConditionService shipConditionService) {
            _shipBreachCondition = shipBreachCondition;
            _shipConditionService = shipConditionService;
        }

        private void OnEnable() {
            _shipBreachCondition.OnBreachesChanged += RefreshRepairVolume;
            RefreshRepairVolume();
        }

        private void OnDisable() =>
            _shipBreachCondition.OnBreachesChanged -= RefreshRepairVolume;

        public override void ServerUse(NetworkIdentity user, GrabController grab) =>
            _shipConditionService.ServerRepairBreach(_point.PointId);
    }
}
