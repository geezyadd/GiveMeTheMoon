using Features.VfxModule.Effects;
using UnityEngine;
using Zenject;

namespace Features.VfxModule {
    // Effects are made through the container, so an effect prefab may carry components that need injection.
    public sealed class VfxFactory : IVfxFactory {
        private readonly DiContainer _container;

        public VfxFactory(DiContainer container) =>
            _container = container;

        public VfxEffectBase Create(VfxEffectBase prefab, Transform parent) =>
            _container.InstantiatePrefabForComponent<VfxEffectBase>(prefab, parent);
    }
}
