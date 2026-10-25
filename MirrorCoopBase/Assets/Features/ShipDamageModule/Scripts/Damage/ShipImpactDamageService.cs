using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using UnityEngine;

namespace Features.ShipDamageModule.Scripts.Damage {
    // Turns one impact into the ship's candidates (installed modules, hull damage points), lets the rule pick and
    // breaks / opens what it picked. Impacts are rare, so the candidate lists are made per call.
    public sealed class ShipImpactDamageService : IShipImpactDamageService {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipDamageRule _shipDamageRule;
        private readonly IShipDamageRandom _shipDamageRandom;
        private readonly IShipModuleCondition _shipModuleCondition;
        private readonly IShipBreachCondition _shipBreachCondition;
        private readonly IShipConditionService _shipConditionService;
        private readonly IReadOnlyComponentRegistry<ShipHullDamagePoint> _shipHullDamagePointRegistry;

        public ShipImpactDamageService(
            IShipRunBindingModel shipRunBindingModel,
            IShipDamageRule shipDamageRule,
            IShipDamageRandom shipDamageRandom,
            IShipModuleCondition shipModuleCondition,
            IShipBreachCondition shipBreachCondition,
            IShipConditionService shipConditionService,
            IReadOnlyComponentRegistry<ShipHullDamagePoint> shipHullDamagePointRegistry) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipDamageRule = shipDamageRule;
            _shipDamageRandom = shipDamageRandom;
            _shipModuleCondition = shipModuleCondition;
            _shipBreachCondition = shipBreachCondition;
            _shipConditionService = shipConditionService;
            _shipHullDamagePointRegistry = shipHullDamagePointRegistry;
        }

        public void ServerApplyImpact(Vector3 point, float strength) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null)
                return;

            if (_shipDamageRule.TryPickBrokenModule(CollectModules(ship), point, strength, _shipDamageRandom.Roll(), out string socketId))
                _shipConditionService.ServerBreakModule(socketId);

            if (_shipDamageRule.TryPickBreach(CollectDamagePoints(), _shipBreachCondition.OpenCount, point, strength, _shipDamageRandom.Roll(), out string pointId))
                _shipConditionService.ServerOpenBreach(pointId);
        }

        private List<ShipDamageCandidate> CollectModules(ShipBase ship) {
            List<ShipDamageCandidate> modules = new List<ShipDamageCandidate>(ship.Sockets.Length);
            foreach (ShipSocket socket in ship.Sockets) {
                if (socket.TryGetInstalledModule(out ModuleDefinition module) == false)
                    continue;

                bool isBreakable = module.CanBreak && _shipModuleCondition.IsWorking(socket.SocketId);
                modules.Add(new ShipDamageCandidate(socket.SocketId, socket.transform.position, isBreakable, module.BreakChanceMultiplier));
            }

            return modules;
        }

        private List<ShipDamageCandidate> CollectDamagePoints() {
            List<ShipDamageCandidate> points = new List<ShipDamageCandidate>(_shipHullDamagePointRegistry.Items.Count);
            foreach (ShipHullDamagePoint point in _shipHullDamagePointRegistry.Items) {
                bool isFree = _shipBreachCondition.IsOpen(point.PointId) == false;
                points.Add(new ShipDamageCandidate(point.PointId, point.transform.position, isFree, 1f));
            }

            return points;
        }
    }
}
