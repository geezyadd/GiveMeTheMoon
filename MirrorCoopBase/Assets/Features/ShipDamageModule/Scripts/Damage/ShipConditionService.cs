using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Hull;

namespace Features.ShipDamageModule.Scripts.Damage {
    // Writes the ship's condition to the synced model and keeps the stats in step: a broken module's modifiers are
    // suspended, a repair resumes them; a repaired breach gives hull HP back.
    public sealed class ShipConditionService : IShipConditionService {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipModuleCondition _shipModuleCondition;
        private readonly IShipConditionWriter _shipConditionWriter;
        private readonly IShipModuleWorkingState _shipModuleWorkingState;
        private readonly IShipHullRepair _shipHullRepair;
        private readonly ShipDamageConfiguration _shipDamageConfiguration;
        private readonly IReadOnlyComponentRegistry<ShipHullDamagePoint> _shipHullDamagePointRegistry;

        public ShipConditionService(
            IShipRunBindingModel shipRunBindingModel,
            IShipModuleCondition shipModuleCondition,
            IShipConditionWriter shipConditionWriter,
            IShipModuleWorkingState shipModuleWorkingState,
            IShipHullRepair shipHullRepair,
            ShipDamageConfiguration shipDamageConfiguration,
            IReadOnlyComponentRegistry<ShipHullDamagePoint> shipHullDamagePointRegistry) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipModuleCondition = shipModuleCondition;
            _shipConditionWriter = shipConditionWriter;
            _shipModuleWorkingState = shipModuleWorkingState;
            _shipHullRepair = shipHullRepair;
            _shipDamageConfiguration = shipDamageConfiguration;
            _shipHullDamagePointRegistry = shipHullDamagePointRegistry;
        }

        public bool ServerBreakModule(string socketId) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || IsBreakableModule(ship, socketId) == false || _shipModuleCondition.IsWorking(socketId) == false)
                return false;

            _shipConditionWriter.ServerAddBrokenSockets(socketId);
            _shipModuleWorkingState.ServerSetModuleWorking(socketId, false);
            return true;
        }

        public bool ServerRepairModule(string socketId) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || _shipConditionWriter.ServerRemoveBrokenSockets(socketId) == false)
                return false;

            _shipModuleWorkingState.ServerSetModuleWorking(socketId, true);
            return true;
        }

        public bool ServerOpenBreach(string pointId) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || HasDamagePoint(pointId) == false || _shipConditionWriter.ServerAddOpenBreaches(pointId) == false)
                return false;

            return true;
        }

        public bool ServerRepairBreach(string pointId) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || _shipConditionWriter.ServerRemoveOpenBreaches(pointId) == false)
                return false;

            _shipHullRepair.ServerHeal(_shipDamageConfiguration.HullPerRepairedBreach);
            return true;
        }

        public void ServerRepairAll() {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null)
                return;

            foreach (ShipSocket socket in ship.Sockets) {
                if (_shipConditionWriter.ServerRemoveBrokenSockets(socket.SocketId))
                    _shipModuleWorkingState.ServerSetModuleWorking(socket.SocketId, true);
            }

            _shipConditionWriter.ServerClearOpenBreaches();
        }

        private static bool IsBreakableModule(ShipBase ship, string socketId) {
            foreach (ShipSocket socket in ship.Sockets) {
                if (socket.SocketId == socketId)
                    return socket.TryGetInstalledModule(out ModuleDefinition module) && module.CanBreak;
            }

            return false;
        }

        private bool HasDamagePoint(string pointId) {
            foreach (ShipHullDamagePoint point in _shipHullDamagePointRegistry.Items) {
                if (point.PointId == pointId)
                    return true;
            }

            return false;
        }
    }
}
