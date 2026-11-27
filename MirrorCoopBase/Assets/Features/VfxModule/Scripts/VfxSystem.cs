using UnityEngine;
using Zenject;

namespace Features.VfxModule {
    // Ages every playing effect once a frame, after the gameplay moved what they follow.
    public sealed class VfxSystem : ILateTickable {
        private readonly IVfxPoolRegistry _registry;

        public VfxSystem(IVfxPoolRegistry registry) =>
            _registry = registry;

        public void LateTick() {
            foreach (VfxPool pool in _registry.All)
                pool.Tick(Time.deltaTime);
        }
    }
}
