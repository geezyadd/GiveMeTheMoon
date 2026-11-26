using System.Collections.Generic;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Projectiles {
    // The projectiles of every kind on this peer, kept for reuse: no Instantiate or Destroy while firing. They live
    // under one root kept across scene changes; if it is gone anyway (play mode ended), the pool starts over.
    public sealed class ProjectilePool {
        private const string ROOT_NAME = "Projectiles";

        private readonly Dictionary<Projectile, Stack<Projectile>> _freeByPrefab = new();
        private readonly Dictionary<Projectile, Projectile> _prefabByProjectile = new();
        private readonly List<Projectile> _active = new();
        private Transform _root;

        public IReadOnlyList<Projectile> Active => _active;

        public Projectile Get(Projectile prefab) {
            EnsureRoot();
            if (_freeByPrefab.TryGetValue(prefab, out Stack<Projectile> free) == false) {
                free = new Stack<Projectile>();
                _freeByPrefab.Add(prefab, free);
            }

            Projectile projectile = free.Count > 0 ? free.Pop() : Create(prefab);
            _active.Add(projectile);
            return projectile;
        }

        public void Release(Projectile projectile) {
            if (_active.Remove(projectile) == false)
                return;

            projectile.OnDespawned();
            if (_prefabByProjectile.TryGetValue(projectile, out Projectile prefab))
                _freeByPrefab[prefab].Push(projectile);
        }

        public bool TryFindActive(ulong id, out Projectile projectile) {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].Id == id) {
                    projectile = _active[i];
                    return true;
                }

            projectile = null;
            return false;
        }

        private Projectile Create(Projectile prefab) {
            Projectile projectile = Object.Instantiate(prefab, _root);
            projectile.gameObject.SetActive(false);
            _prefabByProjectile.Add(projectile, prefab);
            return projectile;
        }

        private void EnsureRoot() {
            if (_root != null)
                return;

            _freeByPrefab.Clear();
            _prefabByProjectile.Clear();
            _active.Clear();
            _root = new GameObject(ROOT_NAME).transform;
            // A scene change (lobby -> game) must not take projectiles in flight with it.
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(_root.gameObject);
        }
    }
}
