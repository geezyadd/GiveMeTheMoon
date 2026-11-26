using System;
using System.Collections.Generic;
using Features.ExplosionModule;
using Features.ExplosionModule.Configuration;
using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Projectiles {
    // Every projectile that ends on this peer, the server's or a copy, shows its explosion's effect and shake where it
    // is, by itself: the look of an explosion never goes over the network. Only the server's projectile sets off the
    // explosion's wave (IExplosionService, without the effect, which each peer's projectile already showed).
    public sealed class ProjectileSpawner : IProjectileSpawner {
        private readonly ProjectilePool _pool;
        private readonly IExplosionService _explosionService;
        private readonly IExplosionEffects _explosionEffects;
        private readonly Dictionary<ulong, Action<ulong>> _onEndedById = new();

        public ProjectileSpawner(ProjectilePool pool, IExplosionService explosionService, IExplosionEffects explosionEffects) {
            _pool = pool;
            _explosionService = explosionService;
            _explosionEffects = explosionEffects;
        }

        public void ServerSpawn(ProjectileDefinition definition, Vector3 position, Vector3 velocity, ulong id,
                                IReadOnlyList<Collider> ignored, Action<ulong> onEnded) {
            _onEndedById[id] = onEnded;
            Spawn(definition, position, velocity, id, isAuthoritative: true, ignored);
        }

        public void SpawnCopy(ProjectileDefinition definition, Vector3 position, Vector3 velocity, ulong id, IReadOnlyList<Collider> ignored) =>
            Spawn(definition, position, velocity, id, isAuthoritative: false, ignored);

        // The server's projectile went off before this copy ended by itself: the copy goes off where it is now.
        public void EndCopy(ulong id) {
            if (_pool.TryFindActive(id, out Projectile projectile) && projectile.IsAuthoritative == false)
                End(projectile, projectile.transform.position);
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

        private void HandleEnded(Projectile projectile, Vector3 point) {
            bool isAuthoritative = projectile.IsAuthoritative;
            ExplosionPreset explosion = projectile.Definition.Explosion;
            ulong id = projectile.Id;
            End(projectile, point);
            if (isAuthoritative == false)
                return;

            if (explosion != null)
                _explosionService.ServerExplode(point, explosion, showsEffect: false);

            if (_onEndedById.Remove(id, out Action<ulong> onEnded))
                onEnded?.Invoke(id);
        }

        private void End(Projectile projectile, Vector3 point) {
            ExplosionPreset explosion = projectile.Definition != null ? projectile.Definition.Explosion : null;
            projectile.Ended -= HandleEnded;
            _pool.Release(projectile);
            if (explosion != null)
                _explosionEffects.Play(point, explosion);
        }
    }
}
