using Features.ExplosionModule.Configuration;
using UnityEngine;

namespace Features.ExplosionModule {
    // What an explosion looks and feels like on this peer: its effect and its screen shake. Local, never over the
    // network: each peer plays it where it sees the explosion go off.
    public interface IExplosionEffects {
        public void Play(Vector3 center, ExplosionPreset preset);
    }
}
