using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Features.WeaponModule.Scripts.Projectiles {
    // Steps every projectile in flight on this peer once per physics step.
    public sealed class ProjectileLifetimeSystem : IFixedTickable {
        private readonly ProjectilePool _pool;

        public ProjectileLifetimeSystem(ProjectilePool pool) =>
            _pool = pool;

        // A projectile that ends in its step goes back to its pool at once, so each list is walked from its end.
        public void FixedTick() {
            IReadOnlyList<ProjectilePrefabPool> pools = _pool.Pools;
            for (int p = 0; p < pools.Count; p++) {
                IReadOnlyList<Projectile> projectiles = pools[p].ActiveProjectiles;
                for (int i = projectiles.Count - 1; i >= 0; i--)
                    if (i < projectiles.Count)
                        projectiles[i].Tick(Time.fixedDeltaTime);
            }
        }
    }
}
