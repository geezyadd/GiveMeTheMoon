using Features.PoolModule;
using UnityEngine;

namespace Features.VfxModule {
    // The instances of one effect: prewarmed on creation, taken free or made new up to the definition's maximum, past
    // it the oldest free-standing one is reused; finished ones go back every frame.
    public sealed class VfxPool : PoolBase<VfxInstance> {
        private readonly VfxDefinition _definition;
        private readonly IVfxFactory _factory;
        private readonly Transform _root;

        public VfxPool(VfxDefinition definition, IVfxFactory factory, Transform root) {
            _definition = definition;
            _factory = factory;
            _root = root;
            Prewarm(definition.PrewarmCount);
        }

        protected override int MaxSize => _definition.MaxCount;

        public VfxInstance Spawn(Vector3 position, Quaternion rotation, float scale, Transform target) {
            VfxInstance instance = Get();
            instance.Play(position, rotation, scale, target);
            return instance;
        }

        public void Tick(float deltaTime) {
            for (int i = Active.Count - 1; i >= 0; i--) {
                VfxInstance instance = Active[i];
                if (instance.IsDestroyed) {
                    Forget(instance);
                    continue;
                }

                instance.Tick(deltaTime);
                if (instance.IsFinished(_definition.MinLifetime, _definition.MaxLifetime))
                    Release(instance);
            }
        }

        protected override VfxInstance Create() {
            VfxInstance instance = new(_factory.Create(_definition.Prefab, _root));
            instance.Deactivate();
            return instance;
        }

        protected override void OnRelease(VfxInstance instance) =>
            instance.Deactivate();

        protected override bool IsAlive(VfxInstance instance) =>
            instance.IsDestroyed == false;

        protected override int GetRecycleIndex() {
            for (int i = 0; i < Active.Count; i++)
                if (Active[i].IsAttached == false)
                    return i;

            return 0;
        }
    }
}
