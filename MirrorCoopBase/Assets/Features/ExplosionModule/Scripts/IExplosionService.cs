using Features.ExplosionModule.Configuration;
using UnityEngine;

namespace Features.ExplosionModule {
    // Sets off explosions. Server only: the explosion goes to every peer, and each runs its wave from the centre.
    public interface IExplosionService {
        public void ServerExplode(Vector3 center, ExplosionPreset preset);
    }
}
