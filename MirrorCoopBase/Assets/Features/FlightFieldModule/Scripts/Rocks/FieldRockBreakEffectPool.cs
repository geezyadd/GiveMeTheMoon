using Features.EffectPoolModule.Scripts;
using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Break effects made once and played again, under the parent the caller gives.
    public sealed class FieldRockBreakEffectPool : IFieldRockBreakEffectPool {
        private readonly FieldRockDurabilityConfiguration _fieldRockDurabilityConfiguration;
        private readonly SelfDisablingEffectPool<FieldRockBreakEffect> _effects = new();

        public FieldRockBreakEffectPool(FieldRockDurabilityConfiguration fieldRockDurabilityConfiguration) =>
            _fieldRockDurabilityConfiguration = fieldRockDurabilityConfiguration;

        public void Play(Transform parent, Vector3 position, float diameter, Material rockMaterial) =>
            _effects.TakeFree(_fieldRockDurabilityConfiguration.BreakEffect, parent).Play(position, diameter, rockMaterial);
    }
}
