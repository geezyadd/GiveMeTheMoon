using System;
using Mirror;

namespace Features.ShipModule.Scripts {
    internal sealed class ShipModules : IShipModules {
        private readonly ShipSocket[] _sockets;
        private readonly ShipStatSheet _stats;
        private readonly ShipStatsSync _statsSync;
        // Read on use: the catalog and the configuration can be injected after the parts are created.
        private readonly Func<EngineCatalog> _engines;
        private readonly Func<ShipAccumulativeStatsConfiguration> _statsConfiguration;

        internal ShipModules(
            ShipSocket[] sockets,
            ShipStatSheet stats,
            ShipStatsSync statsSync,
            Func<EngineCatalog> engines,
            Func<ShipAccumulativeStatsConfiguration> statsConfiguration) {
            _sockets = sockets;
            _stats = stats;
            _statsSync = statsSync;
            _engines = engines;
            _statsConfiguration = statsConfiguration;
        }

        public void ServerOnModuleInstalled(ShipSocket socket) {
            if (NetworkServer.active == false || socket == null)
                return;

            if (_engines().TryGet(socket.InstalledView, out EngineCatalog.EngineStats engine) == false)
                return;

            if (engine.FlightSpeed <= 0f)
                return;

            _stats.SetModuleFlightSpeed(socket.SocketId, engine.FlightSpeed);
            ServerPublishStats();
        }

        public void ServerOnModuleUninstalled(ShipSocket socket) {
            if (socket == null)
                return;

            if (_stats.RemoveModule(socket.SocketId))
                ServerPublishStats();
        }

        // The only writer of the ShipStats model: every change of the stat sheet on the server ends here.
        public void ServerPublishStats() =>
            _statsSync.ServerSetState(_stats.CreateState());

        public float GetStatFull(ShipStatType type) =>
            _stats.GetFull(type);

        public bool HasControlModule() {
            for (int i = 0; i < _sockets.Length; i++) {
                ShipSocket socket = _sockets[i];
                if (socket != null && socket.AcceptedType == ShipModuleType.Control && socket.IsOccupied)
                    return true;
            }

            return false;
        }

        internal void ApplyDefaultStats() =>
            _stats.ApplyDefaults(_statsConfiguration().Defaults);

        internal void ClearInstalledModules() {
            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerClearInstall();
            }
        }
    }
}
