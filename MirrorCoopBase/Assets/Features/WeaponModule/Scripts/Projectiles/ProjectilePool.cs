using System.Collections.Generic;
using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;
using Zenject;

namespace Features.WeaponModule.Scripts.Projectiles {
    // The projectiles of every kind on this peer, kept for reuse: one pool per prefab, prewarmed at start for every
    // projectile in the weapon catalog, so firing does not instantiate. They live under one root kept across scene
    // changes; if it is gone anyway (play mode ended), the pools start over.
    public sealed class ProjectilePool : IInitializable {
        private const string ROOT_NAME = "Projectiles";

        private readonly WeaponCatalog _weaponCatalog;
        private readonly Dictionary<Projectile, ProjectilePrefabPool> _poolByPrefab = new();
        private readonly List<ProjectilePrefabPool> _pools = new();
        private Transform _root;

        public ProjectilePool(WeaponCatalog weaponCatalog) =>
            _weaponCatalog = weaponCatalog;

        public IReadOnlyList<ProjectilePrefabPool> Pools => _pools;

        public void Initialize() {
            foreach (WeaponDefinition weapon in _weaponCatalog.Weapons) {
                ProjectileDefinition projectile = weapon != null ? weapon.Projectile : null;
                if (projectile != null && projectile.Prefab != null)
                    GetPool(projectile.Prefab).PrewarmUpTo(projectile.PrewarmCount);
            }
        }

        public Projectile Get(Projectile prefab) =>
            GetPool(prefab).Take();

        // An active projectile always has its definition, so it knows the pool it came from.
        public void Release(Projectile projectile) {
            if (projectile.Definition != null && _poolByPrefab.TryGetValue(projectile.Definition.Prefab, out ProjectilePrefabPool pool))
                pool.Return(projectile);
        }

        public bool TryFindActive(ulong id, out Projectile projectile) {
            for (int p = 0; p < _pools.Count; p++) {
                IReadOnlyList<Projectile> active = _pools[p].ActiveProjectiles;
                for (int i = 0; i < active.Count; i++)
                    if (active[i].Id == id) {
                        projectile = active[i];
                        return true;
                    }
            }

            projectile = null;
            return false;
        }

        private ProjectilePrefabPool GetPool(Projectile prefab) {
            EnsureRoot();
            if (_poolByPrefab.TryGetValue(prefab, out ProjectilePrefabPool pool) == false) {
                pool = new ProjectilePrefabPool(prefab, _root);
                _poolByPrefab.Add(prefab, pool);
                _pools.Add(pool);
            }

            return pool;
        }

        private void EnsureRoot() {
            if (_root != null)
                return;

            _poolByPrefab.Clear();
            _pools.Clear();
            _root = new GameObject(ROOT_NAME).transform;
            // A scene change (lobby -> game) must not take projectiles in flight with it.
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(_root.gameObject);
        }
    }
}
