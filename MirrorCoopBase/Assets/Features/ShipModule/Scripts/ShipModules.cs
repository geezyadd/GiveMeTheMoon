using System.Collections.Generic;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity;
using Mirror;

namespace Features.ShipModule.Scripts {
    internal sealed class ShipModules : IShipModules {
        private readonly ShipSocket[] _sockets;
        private readonly ShipStatEntity _stats;
        // Read on use: the catalog can be injected after the parts are created.
        private readonly System.Func<EngineCatalog> _engines;
        private readonly Dictionary<ShipSocket, StatModifier> _flightSpeedModifiers = new Dictionary<ShipSocket, StatModifier>();

        internal ShipModules(ShipSocket[] sockets, ShipStatEntity stats, System.Func<EngineCatalog> engines) {
            _sockets = sockets;
            _stats = stats;
            _engines = engines;
        }

        public void ServerOnModuleInstalled(ShipSocket socket) {
            if (NetworkServer.active == false || socket == null)
                return;

            if (_engines().TryGet(socket.InstalledView, out EngineCatalog.EngineStats engine) == false)
                return;

            if (engine.FlightSpeed <= 0f)
                return;

            ServerClearFlightSpeedModifier(socket);
            _stats.GetStat(ShipStatType.FlightSpeed);
            StatModifier modifier = new StatModifier(engine.FlightSpeed, ModifierType.Flat);
            _stats.AddModifier(ShipStatType.FlightSpeed, modifier);
            _flightSpeedModifiers[socket] = modifier;
        }

        public void ServerOnModuleUninstalled(ShipSocket socket) {
            ServerClearFlightSpeedModifier(socket);
        }

        public float GetStatFull(ShipStatType type) =>
            _stats.GetStat(type).FullValue;

        public bool HasControlModule() {
            for (int i = 0; i < _sockets.Length; i++) {
                ShipSocket socket = _sockets[i];
                if (socket != null && socket.AcceptedType == ShipModuleType.Control && socket.IsOccupied)
                    return true;
            }

            return false;
        }

        internal void ApplyDefaultStats() {
            IStat thrust = _stats.GetStat(ShipStatType.Thrust);
            thrust.MaxValue = 999f;

            IStat flightSpeed = _stats.GetStat(ShipStatType.FlightSpeed);
            flightSpeed.MaxValue = 99f;
            flightSpeed.OverrideValue(1f);

            IStat dodge = _stats.GetStat(ShipStatType.DodgeRange);
            dodge.MaxValue = 20f;
            dodge.OverrideValue(1f);

            IStat handling = _stats.GetStat(ShipStatType.Handling);
            handling.MaxValue = 20f;
            handling.OverrideValue(1f);

            IStat armor = _stats.GetStat(ShipStatType.Armor);
            armor.MaxValue = 100f;
            armor.OverrideValue(100f);
        }

        internal void ClearInstalledModules() {
            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerClearInstall();
            }
        }

        private void ServerClearFlightSpeedModifier(ShipSocket socket) {
            if (socket == null)
                return;

            if (_flightSpeedModifiers.TryGetValue(socket, out StatModifier modifier) == false)
                return;

            _flightSpeedModifiers.Remove(socket);
            _stats.RemoveModifier(ShipStatType.FlightSpeed, modifier);
        }
    }
}
