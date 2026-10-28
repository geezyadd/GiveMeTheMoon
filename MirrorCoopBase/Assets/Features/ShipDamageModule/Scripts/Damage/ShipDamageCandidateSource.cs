using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;

namespace Features.ShipDamageModule.Scripts.Damage {
    // The ship's installed modules (by socket) and hull damage points as damage candidates in world space.
    public sealed class ShipDamageCandidateSource : IShipDamageCandidateSource {
        private const float SAME_CHANCE = 1f;

        private readonly IShipModuleCondition _shipModuleCondition;
        private readonly IShipBreachCondition _shipBreachCondition;
        private readonly IReadOnlyComponentRegistry<ShipHullDamagePoint> _shipHullDamagePointRegistry;

        public ShipDamageCandidateSource(
            IShipModuleCondition shipModuleCondition,
            IShipBreachCondition shipBreachCondition,
            IReadOnlyComponentRegistry<ShipHullDamagePoint> shipHullDamagePointRegistry) {
            _shipModuleCondition = shipModuleCondition;
            _shipBreachCondition = shipBreachCondition;
            _shipHullDamagePointRegistry = shipHullDamagePointRegistry;
        }

        public void CollectModules(ShipBase ship, List<ShipDamageCandidate> modules) {
            modules.Clear();
            foreach (ShipSocket socket in ship.Sockets) {
                if (socket.TryGetInstalledModule(out ModuleDefinition module) == false)
                    continue;

                bool isBreakable = module.CanBreak && _shipModuleCondition.IsWorking(socket.SocketId);
                modules.Add(new ShipDamageCandidate(socket.SocketId, socket.transform.position, isBreakable, module.BreakChanceMultiplier));
            }
        }

        public void CollectDamagePoints(List<ShipDamageCandidate> points) {
            points.Clear();
            foreach (ShipHullDamagePoint point in _shipHullDamagePointRegistry.Items) {
                bool isFree = _shipBreachCondition.IsOpen(point.PointId) == false;
                points.Add(new ShipDamageCandidate(point.PointId, point.transform.position, isFree, SAME_CHANCE));
            }
        }
    }
}
