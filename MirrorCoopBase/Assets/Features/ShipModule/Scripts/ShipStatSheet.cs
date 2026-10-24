using System.Collections.Generic;
using Features.ShipModule.Scripts.Generated;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity;

namespace Features.ShipModule.Scripts {
    // The server's source of truth for the ship stats: base values plus the modifiers of the installed modules.
    public sealed class ShipStatSheet {
        private readonly ShipStatEntity _stats;
        private readonly Dictionary<string, KeyValuePair<ShipStatType, StatModifier>[]> _moduleModifiers =
            new Dictionary<string, KeyValuePair<ShipStatType, StatModifier>[]>();

        public ShipStatSheet(ShipStatEntity stats) =>
            _stats = stats;

        // ShipStatsConfigurationValidator guarantees every stat is listed once. The cap is set before the value:
        // an accumulative stat clamps the value to the cap.
        public void ApplyDefaults(IReadOnlyList<ShipAccumulativeStatsConfiguration.ShipStatDefault> defaults) {
            for (int i = 0; i < defaults.Count; i++) {
                ShipAccumulativeStatsConfiguration.ShipStatDefault statDefault = defaults[i];
                IStat stat = _stats.GetStat(statDefault.Type);
                stat.MaxValue = statDefault.MaxValue;
                stat.OverrideValue(statDefault.Value);
            }
        }

        // Remembers which modifiers came from which socket, so removing a module takes off exactly its own.
        public void AddModule(string socketId, IReadOnlyList<ModuleDefinition.StatModifierEntry> modifiers) {
            RemoveModule(socketId);
            KeyValuePair<ShipStatType, StatModifier>[] applied = new KeyValuePair<ShipStatType, StatModifier>[modifiers.Count];
            for (int i = 0; i < modifiers.Count; i++) {
                ModuleDefinition.StatModifierEntry entry = modifiers[i];
                StatModifier modifier = new StatModifier(entry.Value, entry.Type);
                _stats.AddModifier(entry.Stat, modifier);
                applied[i] = new KeyValuePair<ShipStatType, StatModifier>(entry.Stat, modifier);
            }

            _moduleModifiers[socketId] = applied;
        }

        public bool RemoveModule(string socketId) {
            if (_moduleModifiers.Remove(socketId, out KeyValuePair<ShipStatType, StatModifier>[] applied) == false)
                return false;

            for (int i = 0; i < applied.Length; i++)
                _stats.RemoveModifier(applied[i].Key, applied[i].Value);

            return true;
        }

        public float GetFull(ShipStatType type) =>
            _stats.GetStat(type).FullValue;

        public float GetMax(ShipStatType type) =>
            _stats.GetStat(type).MaxValue;

        // An accumulative stat stops at its minimum (0).
        public void Subtract(ShipStatType type, float amount) =>
            _stats.GetStat(type).Subtract(amount);

        public void Refill(ShipStatType type) {
            IStat stat = _stats.GetStat(type);
            stat.OverrideValue(stat.MaxValue);
        }

        public ShipStatsState CreateState() =>
            new ShipStatsState {
                Thrust = GetFull(ShipStatType.Thrust),
                DodgeRange = GetFull(ShipStatType.DodgeRange),
                Handling = GetFull(ShipStatType.Handling),
                Armor = GetFull(ShipStatType.Armor),
                FlightSpeed = GetFull(ShipStatType.FlightSpeed),
                Hull = GetFull(ShipStatType.Hull),
                HullMax = GetMax(ShipStatType.Hull),
                SonarRange = GetFull(ShipStatType.SonarRange)
            };
    }
}
