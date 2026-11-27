using UnityEngine;

namespace Features.VfxModule.Effects {
    // An effect drawn by a particle system and its children. A pack's demo prefab often loops; with Play Once on, every
    // system of this instance runs one cycle, so the effect ends and goes back to its pool.
    public sealed class ParticleSystemVfx : VfxEffectBase {
        [SerializeField] private ParticleSystem _particleSystem;
        [Tooltip("Turns looping off in every system of this instance, so it plays once and ends.")]
        [SerializeField] private bool _playOnce = true;

        public override bool IsAlive => _particleSystem.IsAlive(true);

        private void Awake() {
            if (_playOnce == false)
                return;

            foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>(true)) {
                ParticleSystem.MainModule main = system.main;
                main.loop = false;
            }
        }

        public override void Play() {
            _particleSystem.Clear(true);
            _particleSystem.Play(true);
        }

        public override void Stop() =>
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
