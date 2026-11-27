using UnityEngine;

namespace Features.VfxModule.Effects {
    // One visual effect on a prefab, whatever draws it: a particle system (ParticleSystemVfx) or a VFX Graph
    // (VisualEffectVfx). The pool only plays it, stops it and asks whether anything of it is still alive.
    public abstract class VfxEffectBase : MonoBehaviour {
        public abstract bool IsAlive { get; }

        public abstract void Play();

        public abstract void Stop();
    }
}
