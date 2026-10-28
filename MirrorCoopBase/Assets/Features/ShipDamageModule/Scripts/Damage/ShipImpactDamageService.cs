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
        private readonly IShipBreachCondition _shipBreachCondition;
        private readonly IShipConditionService _shipConditionService;
        private readonly IShipDamageCandidateSource _shipDamageCandidateSource;

        public ShipImpactDamageService(
            IShipRunBindingModel shipRunBindingModel,
            IShipDamageRule shipDamageRule,
            IShipDamageRandom shipDamageRandom,
            IShipBreachCondition shipBreachCondition,
            IShipConditionService shipConditionService,
            IShipDamageCandidateSource shipDamageCandidateSource) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipDamageRule = shipDamageRule;
            _shipDamageRandom = shipDamageRandom;
            _shipBreachCondition = shipBreachCondition;
            _shipConditionService = shipConditionService;
            _shipDamageCandidateSource = shipDamageCandidateSource;
        }

        public void ServerApplyImpact(Vector3 point, float strength) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null)
                return;

            List<ShipDamageCandidate> modules = new List<ShipDamageCandidate>(ship.Sockets.Length);
            _shipDamageCandidateSource.CollectModules(ship, modules);
            if (_shipDamageRule.TryPickBrokenModule(modules, point, strength, _shipDamageRandom.Roll(), out string socketId))
                _shipConditionService.ServerBreakModule(socketId);

            List<ShipDamageCandidate> points = new List<ShipDamageCandidate>();
            _shipDamageCandidateSource.CollectDamagePoints(points);
            if (_shipDamageRule.TryPickBreach(points, _shipBreachCondition.OpenCount, point, strength, _shipDamageRandom.Roll(), out string pointId))
                _shipConditionService.ServerOpenBreach(pointId);
        }
    }
}
