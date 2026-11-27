using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.VfxModule {
    // The pools live under one root kept across scene changes, so an effect playing while a scene unloads finishes.
    public sealed class VfxPoolRegistry : IVfxPoolRegistry, IDisposable {
        private const string ROOT_NAME = "Vfx";

        private readonly IVfxFactory _factory;
        private readonly Dictionary<VfxDefinition, VfxPool> _pools = new();

        private Transform _root;

        public VfxPoolRegistry(IVfxFactory factory) =>
            _factory = factory;

        public IReadOnlyCollection<VfxPool> All => _pools.Values;

        public VfxPool Get(VfxDefinition effect) {
            EnsureRoot();
            if (_pools.TryGetValue(effect, out VfxPool pool) == false) {
                pool = new VfxPool(effect, _factory, _root);
                _pools.Add(effect, pool);
            }

            return pool;
        }

        public void Dispose() {
            _pools.Clear();
            if (_root != null)
                Object.Destroy(_root.gameObject);
        }

        // The root may be gone (play mode ended); the pools start over with a new one.
        private void EnsureRoot() {
            if (_root != null)
                return;

            _pools.Clear();
            _root = new GameObject(ROOT_NAME).transform;
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(_root.gameObject);
        }
    }
}
