using Features.EffectPoolModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // The root of the rock break effect prefab: chunks of the rock's own material and a puff of dust, authored for a
    // rock of 1 m and scaled by the rock's diameter (the particle systems simulate in local space and scale with the
    // hierarchy). The root system's stop action disables the object when the effect is over, so a disabled effect
    // is free for the pool.
    public sealed class FieldRockBreakEffect : MonoBehaviour, ISelfDisablingEffect {
        [SerializeField] private ParticleSystem _particles;
        [SerializeField] private ParticleSystemRenderer _chunkRenderer;

        public bool IsPlaying => gameObject.activeSelf;

        public void Play(Vector3 position, float diameter, Material rockMaterial) {
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one * diameter;
            _chunkRenderer.sharedMaterial = rockMaterial;
            gameObject.SetActive(true);
            _particles.Play(true);
        }
    }
}
