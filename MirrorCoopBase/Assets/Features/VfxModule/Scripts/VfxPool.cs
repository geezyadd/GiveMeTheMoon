using System.Collections.Generic;
using UnityEngine;

namespace Features.VfxModule {
    // The instances of one effect: prewarmed on creation, taken free or made new up to the definition's maximum, past
    // it the oldest free-standing one is reused; finished ones go back every frame.
    public sealed class VfxPool {
        private readonly VfxDefinition _definition;
        private readonly IVfxFactory _factory;
        private readonly Transform _root;
        private readonly List<VfxInstance> _free = new();
        private readonly List<VfxInstance> _active = new();

        public VfxPool(VfxDefinition definition, IVfxFactory factory, Transform root) {
            _definition = definition;
            _factory = factory;
            _root = root;
            for (int i = 0; i < definition.PrewarmCount; i++)
                _free.Add(Create());
        }

        public VfxInstance Spawn(Vector3 position, Quaternion rotation, float scale, Transform target) {
            VfxInstance instance = Take();
            instance.Play(position, rotation, scale, target);
            _active.Add(instance);
            return instance;
        }

        public void Tick(float deltaTime) {
            for (int i = _active.Count - 1; i >= 0; i--) {
                VfxInstance instance = _active[i];
                if (instance.IsDestroyed) {
                    _active.RemoveAt(i);
                    continue;
                }

                instance.Tick(deltaTime);
                if (instance.IsFinished(_definition.MinLifetime, _definition.MaxLifetime)) {
                    instance.Deactivate();
                    _active.RemoveAt(i);
                    _free.Add(instance);
                }
            }
        }

        private VfxInstance Take() {
            while (_free.Count > 0) {
                VfxInstance free = _free[_free.Count - 1];
                _free.RemoveAt(_free.Count - 1);
                if (free.IsDestroyed == false)
                    return free;
            }

            if (_active.Count < _definition.MaxCount)
                return Create();

            int index = FindRecycleIndex();
            VfxInstance reused = _active[index];
            _active.RemoveAt(index);
            reused.Deactivate();
            return reused;
        }

        private int FindRecycleIndex() {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].IsAttached == false)
                    return i;

            return 0;
        }

        private VfxInstance Create() {
            VfxInstance instance = new(_factory.Create(_definition.Prefab, _root));
            instance.Deactivate();
            return instance;
        }
    }
}
