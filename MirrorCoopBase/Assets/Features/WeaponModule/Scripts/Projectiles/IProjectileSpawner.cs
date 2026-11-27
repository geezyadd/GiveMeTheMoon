using System;
using System.Collections.Generic;
using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Projectiles {
    // Launches projectiles on this peer. The server's are real and go off; every other peer's are copies for the eye.
    public interface IProjectileSpawner {
        // Server only: it goes off where it ends, then onEnded gets its id.
        public void ServerSpawn(ProjectileDefinition definition, Vector3 position, Vector3 velocity, ulong id,
                                IReadOnlyList<Collider> ignored, Action<ulong> onEnded);

        public void SpawnCopy(ProjectileDefinition definition, Vector3 position, Vector3 velocity, ulong id, IReadOnlyList<Collider> ignored);

        // The server's projectile of this id went off: the copy goes away.
        public void EndCopy(ulong id);
    }
}
