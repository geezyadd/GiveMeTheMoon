using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Break effects made once and played again: an effect disables itself when it is over and is then free. A few
    // play at once at most, so finding a free one walks the list.
    public sealed class FieldRockBreakEffectPool : IFieldRockBreakEffectPool {
        private static readonly Predicate<FieldRockBreakEffect> _isDestroyed = IsDestroyed;

        private readonly FieldRockDurabilityConfiguration _fieldRockDurabilityConfiguration;
        private readonly List<FieldRockBreakEffect> _effects = new();

        public FieldRockBreakEffectPool(FieldRockDurabilityConfiguration fieldRockDurabilityConfiguration) =>
            _fieldRockDurabilityConfiguration = fieldRockDurabilityConfiguration;

        public void Play(Transform parent, Vector3 position, float diameter, Material rockMaterial) =>
            TakeFree(parent).Play(position, diameter, rockMaterial);

        // The parent outlives scene loads, but an effect destroyed from outside is dropped.
        private FieldRockBreakEffect TakeFree(Transform parent) {
            _effects.RemoveAll(_isDestroyed);
            foreach (FieldRockBreakEffect effect in _effects) {
                if (!effect.IsPlaying) {
                    effect.transform.SetParent(parent, false);
                    return effect;
                }
            }

            FieldRockBreakEffect created = UnityEngine.Object.Instantiate(_fieldRockDurabilityConfiguration.BreakEffect, parent, false);
            _effects.Add(created);
            return created;
        }

        private static bool IsDestroyed(FieldRockBreakEffect effect) =>
            effect == null;
    }
}
