using Features.EffectPoolModule.Scripts;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts {
    // The root of the rock impact effect prefab: dust and chunks authored for 1 m and scaled to the hit's size. The
    // root system's stop action disables the object when it is over, so a disabled effect is free for the pool.
    public sealed class ShipImpactEffect : MonoBehaviour, ISelfDisablingEffect {
        [SerializeField] private ParticleSystem _particles;

        public bool IsPlaying => gameObject.activeSelf;

        public void Play(Vector3 position, float size) {
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one * size;
            gameObject.SetActive(true);
            _particles.Play(true);
        }
    }
}
