using Features.ShipModule.Scripts.Hull;

namespace Features.ShipModule.Scripts.Damage {
    // Writes the ship's condition to the synced model and keeps the stats in step: a broken module's modifiers are
    // suspended, a repair resumes them; a repaired breach gives hull HP back.
    public sealed class ShipConditionService : IShipConditionService {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipModuleCondition _shipModuleCondition;
        private readonly IShipHullRepair _shipHullRepair;
        private readonly IShipDamageEventPublisher _shipDamageEventPublisher;
        private readonly ShipDamageConfiguration _shipDamageConfiguration;

        public ShipConditionService(
            IShipRunBindingModel shipRunBindingModel,
            IShipModuleCondition shipModuleCondition,
            IShipHullRepair shipHullRepair,
            IShipDamageEventPublisher shipDamageEventPublisher,
            ShipDamageConfiguration shipDamageConfiguration) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipModuleCondition = shipModuleCondition;
            _shipHullRepair = shipHullRepair;
            _shipDamageEventPublisher = shipDamageEventPublisher;
            _shipDamageConfiguration = shipDamageConfiguration;
        }

        public bool ServerBreakModule(string socketId) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || IsBreakableModule(ship, socketId) == false || _shipModuleCondition.IsWorking(socketId) == false)
                return false;

            ship.ConditionSync.ServerAddBrokenSockets(socketId);
            ship.Modules.ServerSetModuleWorking(socketId, false);
            _shipDamageEventPublisher.PublishModuleBroken(socketId);
            return true;
        }

        public bool ServerRepairModule(string socketId) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || ship.ConditionSync.ServerRemoveBrokenSockets(socketId) == false)
                return false;

            ship.Modules.ServerSetModuleWorking(socketId, true);
            _shipDamageEventPublisher.PublishRepaired(ShipRepairKind.Module, socketId);
            return true;
        }

        public bool ServerOpenBreach(string pointId) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || HasDamagePoint(ship, pointId) == false || ship.ConditionSync.ServerAddOpenBreaches(pointId) == false)
                return false;

            _shipDamageEventPublisher.PublishHullBreached(pointId);
            return true;
        }

        public bool ServerRepairBreach(string pointId) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || ship.ConditionSync.ServerRemoveOpenBreaches(pointId) == false)
                return false;

            _shipHullRepair.ServerHeal(_shipDamageConfiguration.HullPerRepairedBreach);
            _shipDamageEventPublisher.PublishRepaired(ShipRepairKind.Breach, pointId);
            return true;
        }

        public void ServerRepairAll() {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null)
                return;

            foreach (ShipSocket socket in ship.Sockets) {
                if (ship.ConditionSync.ServerRemoveBrokenSockets(socket.SocketId))
                    ship.Modules.ServerSetModuleWorking(socket.SocketId, true);
            }

            ship.ConditionSync.ServerClearOpenBreaches();
        }

        private static bool IsBreakableModule(ShipBase ship, string socketId) {
            foreach (ShipSocket socket in ship.Sockets) {
                if (socket.SocketId == socketId)
                    return socket.TryGetInstalledModule(out ModuleDefinition module) && module.CanBreak;
            }

            return false;
        }

        private static bool HasDamagePoint(ShipBase ship, string pointId) {
            foreach (ShipHullDamagePoint point in ship.DamagePoints) {
                if (point.PointId == pointId)
                    return true;
            }

            return false;
        }
    }
}
