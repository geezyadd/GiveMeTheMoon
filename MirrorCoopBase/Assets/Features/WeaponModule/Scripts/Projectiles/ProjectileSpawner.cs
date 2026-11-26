using System;
using System.Collections.Generic;
using Features.ExplosionModule;
using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Projectiles {
    public sealed class ProjectileSpawner : IProjectileSpawner {
        private readonly ProjectilePool _pool;
        private readonly IExplosionService _explosionService;
        private readonly Dictionary<ulong, Action<ulong>> _onEndedById = new();

        public ProjectileSpawner(ProjectilePool pool, IExplosionService explosionService) {
            _pool = pool;
            _explosionService = explosionService;
        }

        public void ServerSpawn(ProjectileDefinition definition, Vector3 position, Vector3 velocity, ulong id,
                                IReadOnlyList<Collider> ignored, Action<ulong> onEnded) {
            _onEndedById[id] = onEnded;
            Spawn(definition, position, velocity, id, isAuthoritative: true, ignored);
        }

        public void SpawnCopy(ProjectileDefinition definition, Vector3 position, Vector3 velocity, ulong id, IReadOnlyList<Collider> ignored) =>
            Spawn(definition, position, velocity, id, isAuthoritative: false, ignored);

        public void EndCopy(ulong id) {
            if (_pool.TryFindActive(id, out Projectile projectile) && projectile.IsAuthoritative == false)
                Release(projectile);
        }

        private void Spawn(ProjectileDefinition definition, Vector3 position, Vector3 velocity, ulong id, bool isAuthoritative,
                           IReadOnlyList<Collider> ignored) {
            if (definition.Prefab == null) {
                Debug.LogError($"Projectile: {definition.name} has no prefab.");
                return;
            }

            Projectile projectile = _pool.Get(definition.Prefab);
            projectile.Ended += HandleEnded;
            projectile.OnSpawned(definition, position, velocity, id, isAuthoritative, ignored);
        }

        // A copy that ends on its own goes back to the pool at once: the server's explosion shows where the real one
        // went off.
        private void HandleEnded(Projectile projectile, Vector3 point) {
            if (projectile.IsAuthoritative) {
                ProjectileDefinition definition = projectile.Definition;
                ulong id = projectile.Id;
                Release(projectile);
                if (definition.Explosion != null)
                    _explosionService.ServerExplode(point, definition.Explosion);

                if (_onEndedById.Remove(id, out Action<ulong> onEnded))
                    onEnded?.Invoke(id);

                return;
            }

            Release(projectile);
        }

        private void Release(Projectile projectile) {
            projectile.Ended -= HandleEnded;
            _pool.Release(projectile);
        }
    }
}
