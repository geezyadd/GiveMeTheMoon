#if VFX_GRAPH_PRESENT
using UnityEngine;
using UnityEngine.VFX;

namespace Features.VfxModule.Effects {
    // An effect drawn by a VFX Graph. Compiled only while the Visual Effect Graph package is in the project.
    public sealed class VisualEffectVfx : VfxEffectBase {
        [SerializeField] private VisualEffect _effect;

        public override bool IsAlive => _effect.aliveParticleCount > 0 || _effect.HasAnySystemAwake();

        public override void Play() =>
            _effect.Reinit();

        public override void Stop() =>
            _effect.Stop();
    }
}
#endif
