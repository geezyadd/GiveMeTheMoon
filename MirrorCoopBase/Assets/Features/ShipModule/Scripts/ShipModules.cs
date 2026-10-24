using System;

namespace Features.ShipModule.Scripts {
    internal sealed class ShipModules : IShipModules {
        private readonly ShipSocket[] _sockets;
        private readonly ShipStatSheet _stats;
        private readonly ShipStatsSync _statsSync;
        private readonly ShipConditionSync _conditionSync;
        // Read on use: the configuration can be injected after the parts are created.
        private readonly Func<ShipAccumulativeStatsConfiguration> _statsConfiguration;

        internal ShipModules(
            ShipSocket[] sockets,
            ShipStatSheet stats,
            ShipStatsSync statsSync,
            ShipConditionSync conditionSync,
            Func<ShipAccumulativeStatsConfiguration> statsConfiguration) {
            _sockets = sockets;
            _stats = stats;
            _statsSync = statsSync;
            _conditionSync = conditionSync;
            _statsConfiguration = statsConfiguration;
        }

        public void ServerOnModuleInstalled(ShipSocket socket, ModuleDefinition module) {
            _stats.AddModule(socket.SocketId, module.StatModifiers);
            ServerPublishStats();
        }

        // A module taken off (or reset) takes its broken state with it: reinstalled, it works again.
        public void ServerOnModuleUninstalled(ShipSocket socket) {
            _conditionSync.ServerRemoveBrokenSockets(socket.SocketId);
            if (_stats.RemoveModule(socket.SocketId))
                ServerPublishStats();
        }

        public void ServerSetModuleWorking(string socketId, bool isWorking) {
            bool isChanged = isWorking ? _stats.ResumeModule(socketId) : _stats.SuspendModule(socketId);
            if (isChanged)
                ServerPublishStats();
        }

        // The only writer of the ShipStats model: every change of the stat sheet on the server ends here.
        public void ServerPublishStats() =>
            _statsSync.ServerSetState(_stats.CreateState());

        public float GetStatFull(ShipStatType type) =>
            _stats.GetFull(type);

        public bool HasControlModule() {
            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i].TryGetInstalledModule(out ModuleDefinition module) && module.IsControlModule)
                    return true;
            }

            return false;
        }

        internal void ApplyDefaultStats() =>
            _stats.ApplyDefaults(_statsConfiguration().Defaults);

        internal void ResetInstalledModules() {
            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerResetInstall();
            }
        }
    }
}
