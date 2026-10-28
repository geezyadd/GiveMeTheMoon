using Features.EffectPoolModule.Scripts;
using UnityEngine;

namespace Features.BugModule.Scripts.View {
    // The root of the bug death effect prefab: a burst of shell bits and a puff (particle systems whose main one
    // disables the object when it stops), so a disabled effect is free for the pool.
    public sealed class BugDeathEffect : MonoBehaviour, ISelfDisablingEffect {
        [SerializeField] private ParticleSystem _particles;

        public bool IsPlaying => gameObject.activeSelf;

        public void Play(Vector3 position) {
            transform.SetPositionAndRotation(position, Quaternion.identity);
            gameObject.SetActive(true);
            _particles.Play(true);
        }
    }
}
