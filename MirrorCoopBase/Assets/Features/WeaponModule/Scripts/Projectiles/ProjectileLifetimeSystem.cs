using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Features.WeaponModule.Scripts.Projectiles {
    // Steps every projectile in flight on this peer once per physics step.
    public sealed class ProjectileLifetimeSystem : IFixedTickable {
        private readonly ProjectilePool _pool;

        public ProjectileLifetimeSystem(ProjectilePool pool) =>
            _pool = pool;

        public void FixedTick() {
            IReadOnlyList<Projectile> projectiles = _pool.Active;
            for (int i = projectiles.Count - 1; i >= 0; i--)
                if (i < projectiles.Count)
                    projectiles[i].Tick(Time.fixedDeltaTime);
        }
    }
}
