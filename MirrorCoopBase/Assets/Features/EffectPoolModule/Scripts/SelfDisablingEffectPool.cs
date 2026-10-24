using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.EffectPoolModule.Scripts {
    // Effects made once and played again: an effect that is over is free. A few play at once at most, so finding a
    // free one walks the list. An effect destroyed from outside (its scene unloaded) is dropped.
    public sealed class SelfDisablingEffectPool<TEffect> where TEffect : Component, ISelfDisablingEffect {
        private static readonly Predicate<TEffect> _isDestroyed = IsDestroyed;

        private readonly List<TEffect> _effects = new();

        // A free effect moved under the parent (null = the scene root), or a new one from the prefab.
        public TEffect TakeFree(TEffect prefab, Transform parent) {
            _effects.RemoveAll(_isDestroyed);
            foreach (TEffect effect in _effects) {
                if (effect.IsPlaying)
                    continue;

                effect.transform.SetParent(parent, false);
                return effect;
            }

            TEffect created = Object.Instantiate(prefab, parent, false);
            _effects.Add(created);
            return created;
        }

        private static bool IsDestroyed(TEffect effect) =>
            effect == null;
    }
}
