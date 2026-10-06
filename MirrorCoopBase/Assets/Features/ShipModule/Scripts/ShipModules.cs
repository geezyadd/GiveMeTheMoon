using System.Collections.Generic;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity;
using Mirror;

namespace Features.ShipModule.Scripts {
    // The modules installed in the ship's sockets, turned into ship stats.
    internal sealed class ShipModules {
        private readonly ShipSocket[] _sockets;
        private readonly ShipStatEntity _stats;
        private readonly EngineCatalog _engines;
        private readonly Dictionary<ShipSocket, StatModifier> _flightSpeedModifiers = new Dictionary<ShipSocket, StatModifier>();

        internal ShipModules(ShipSocket[] sockets, ShipStatEntity stats, EngineCatalog engines) {
            _sockets = sockets;
            _stats = stats;
            _engines = engines;
        }

        internal void ServerOnModuleInstalled(ShipSocket socket) {
            if (NetworkServer.active == false || socket == null || _stats == null || _engines == null)
                return;

            if (_engines.TryGet(socket.InstalledView, out EngineCatalog.EngineStats engine) == false)
                return;

            if (engine.FlightSpeed <= 0f)
                return;

            ServerClearFlightSpeedModifier(socket);
            _stats.GetStat(ShipStatType.FlightSpeed);
            StatModifier modifier = new StatModifier(engine.FlightSpeed, ModifierType.Flat);
            _stats.AddModifier(ShipStatType.FlightSpeed, modifier);
            _flightSpeedModifiers[socket] = modifier;
        }

        internal void ServerOnModuleUninstalled(ShipSocket socket) {
            ServerClearFlightSpeedModifier(socket);
        }

        internal float GetStatFull(ShipStatType type) {
            if (_stats == null)
                return 0f;

            return _stats.GetStat(type).FullValue;
        }

        internal void ApplyDefaultStats() {
            if (_stats == null)
                return;

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

        internal bool HasControlModule() {
            if (_sockets == null)
                return false;

            for (int i = 0; i < _sockets.Length; i++) {
                ShipSocket socket = _sockets[i];
                if (socket != null && socket.AcceptedType == ShipModuleType.Control && socket.IsOccupied)
                    return true;
            }

            return false;
        }

        internal void ClearInstalledModules() {
            if (_sockets == null)
                return;

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerClearInstall();
            }
        }

        private void ServerClearFlightSpeedModifier(ShipSocket socket) {
            if (socket == null || _stats == null)
                return;

            if (_flightSpeedModifiers.TryGetValue(socket, out StatModifier modifier) == false)
                return;

            _flightSpeedModifiers.Remove(socket);
            _stats.RemoveModifier(ShipStatType.FlightSpeed, modifier);
        }
    }
}
