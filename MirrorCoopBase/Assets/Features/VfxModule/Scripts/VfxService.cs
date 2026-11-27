using UnityEngine;

namespace Features.VfxModule {
    public sealed class VfxService : IVfxService {
        private readonly IVfxPoolRegistry _registry;

        public VfxService(IVfxPoolRegistry registry) =>
            _registry = registry;

        public void Play(VfxDefinition effect, Vector3 position, Quaternion rotation, float scale = 1f) {
            if (IsPlayable(effect))
                _registry.Get(effect).Spawn(position, rotation, scale, null);
        }

        public VfxHandle Attach(VfxDefinition effect, Transform target) =>
            IsPlayable(effect) && target != null
                ? new VfxHandle(_registry.Get(effect).Spawn(target.position, target.rotation, 1f, target))
                : default;

        private static bool IsPlayable(VfxDefinition effect) {
            if (effect != null && effect.Prefab != null)
                return true;

            Debug.LogError($"Vfx: {(effect != null ? effect.name : "an empty definition")} has no prefab.");
            return false;
        }
    }
}
