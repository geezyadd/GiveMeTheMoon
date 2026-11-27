using System.Collections.Generic;
using Features.PoolModule;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Projectiles {
    // The projectiles of one prefab on this peer. It never takes one back while it flies: the server's projectile
    // must go off, so the pool grows past its prewarmed ones instead.
    public sealed class ProjectilePrefabPool : PoolBase<Projectile> {
        private readonly Projectile _prefab;
        private readonly Transform _root;

        public ProjectilePrefabPool(Projectile prefab, Transform root) {
            _prefab = prefab;
            _root = root;
        }

        public IReadOnlyList<Projectile> ActiveProjectiles => Active;

        protected override int MaxSize => int.MaxValue;

        public void PrewarmUpTo(int count) =>
            Prewarm(count - All.Count);

        public Projectile Take() =>
            Get();

        public void Return(Projectile projectile) =>
            Release(projectile);

        protected override Projectile Create() {
            Projectile projectile = Object.Instantiate(_prefab, _root);
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        protected override void OnRelease(Projectile projectile) =>
            projectile.OnDespawned();
    }
}
