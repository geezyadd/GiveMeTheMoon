using Features.ExplosionModule.Configuration;
using UnityEngine;

namespace Features.ExplosionModule {
    // Sets off explosions. Server only: the explosion goes to every peer, and each runs its wave from the centre.
    public interface IExplosionService {
        // showsEffect off: the peers run only the wave; whatever set it off shows the effect by itself (a projectile).
        public void ServerExplode(Vector3 center, ExplosionPreset preset, bool showsEffect = true);
    }
}
