using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts.Generated;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity;

namespace Features.ShipModule.Scripts {
    // The server's source of truth for the ship stats: base values plus the modifiers of the installed modules.
    public sealed class ShipStatSheet {
        private readonly ShipStatEntity _stats;
        private readonly Dictionary<string, StatModifier> _flightSpeedModifiers = new Dictionary<string, StatModifier>();

        public ShipStatSheet(ShipStatEntity stats) =>
            _stats = stats;

        public void ApplyDefaults(IReadOnlyList<ShipAccumulativeStatsConfiguration.ShipStatDefault> defaults) {
            HashSet<ShipStatType> applied = new HashSet<ShipStatType>();
            for (int i = 0; i < defaults.Count; i++) {
                ShipAccumulativeStatsConfiguration.ShipStatDefault statDefault = defaults[i];
                if (statDefault.Type == ShipStatType.None || applied.Add(statDefault.Type) == false)
                    throw new InvalidOperationException($"Ship stats configuration has an invalid or duplicate default for {statDefault.Type}.");

                IStat stat = _stats.GetStat(statDefault.Type);
                stat.MaxValue = statDefault.MaxValue;
                stat.OverrideValue(statDefault.Value);
            }

            ShipStatType[] statTypes = (ShipStatType[])Enum.GetValues(typeof(ShipStatType));
            for (int i = 0; i < statTypes.Length; i++) {
                ShipStatType type = statTypes[i];
                if (type != ShipStatType.None && applied.Contains(type) == false)
                    throw new KeyNotFoundException($"Ship stats configuration has no default for {type}.");
            }
        }

        public void SetModuleFlightSpeed(string socketId, float flightSpeed) {
            RemoveModule(socketId);
            StatModifier modifier = new StatModifier(flightSpeed, ModifierType.Flat);
            _stats.AddModifier(ShipStatType.FlightSpeed, modifier);
            _flightSpeedModifiers[socketId] = modifier;
        }

        public bool RemoveModule(string socketId) {
            if (_flightSpeedModifiers.Remove(socketId, out StatModifier modifier) == false)
                return false;

            _stats.RemoveModifier(ShipStatType.FlightSpeed, modifier);
            return true;
        }

        public float GetFull(ShipStatType type) =>
            _stats.GetStat(type).FullValue;

        public ShipStatsState CreateState() =>
            new ShipStatsState {
                Thrust = GetFull(ShipStatType.Thrust),
                DodgeRange = GetFull(ShipStatType.DodgeRange),
                Handling = GetFull(ShipStatType.Handling),
                Armor = GetFull(ShipStatType.Armor),
                FlightSpeed = GetFull(ShipStatType.FlightSpeed)
            };
    }
}
