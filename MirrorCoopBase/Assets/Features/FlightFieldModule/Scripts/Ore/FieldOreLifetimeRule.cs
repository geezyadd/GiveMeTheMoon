using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    public sealed class FieldOreLifetimeRule : IFieldOreLifetimeRule {
        private readonly FieldOreConfiguration _fieldOreConfiguration;

        public FieldOreLifetimeRule(FieldOreConfiguration fieldOreConfiguration) =>
            _fieldOreConfiguration = fieldOreConfiguration;

        public bool IsExpired(in FieldOreEntry entry, double now) =>
            !IsHooked(entry) && now >= entry.ExpiresAt;

        public float GetVisibleShare(in FieldOreEntry entry, double now) {
            if (IsHooked(entry))
                return 1f;

            if (_fieldOreConfiguration.FadeSeconds <= 0f)
                return now >= entry.ExpiresAt ? 0f : 1f;

            return Mathf.Clamp01((float)(entry.ExpiresAt - now) / _fieldOreConfiguration.FadeSeconds);
        }

        private static bool IsHooked(in FieldOreEntry entry) =>
            !string.IsNullOrEmpty(entry.HookerId);
    }
}
