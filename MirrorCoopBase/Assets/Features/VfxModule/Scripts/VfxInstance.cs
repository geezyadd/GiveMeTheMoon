using Features.VfxModule.Effects;
using UnityEngine;

namespace Features.VfxModule {
    // One pooled effect object: played at a point or after a target, aged every frame, finished once it is old enough
    // and nothing of it is alive (or too old), then deactivated for the next play.
    public sealed class VfxInstance {
        private readonly VfxEffectBase _effect;

        private Transform _target;
        private float _age;

        public VfxInstance(VfxEffectBase effect) =>
            _effect = effect;

        public int Generation { get; private set; }
        public bool IsAttached { get; private set; }
        public bool IsDestroyed => _effect == null;

        public void Play(Vector3 position, Quaternion rotation, float scale, Transform target) {
            _target = target;
            IsAttached = target != null;
            _age = 0f;
            Transform effectTransform = _effect.transform;
            effectTransform.SetPositionAndRotation(position, rotation);
            effectTransform.localScale = Vector3.one * scale;
            _effect.gameObject.SetActive(true);
            _effect.Play();
        }

        public void Stop() {
            _target = null;
            IsAttached = false;
            _effect.Stop();
        }

        public void Tick(float deltaTime) {
            _age += deltaTime;
            if (IsAttached == false)
                return;

            if (_target == null) {
                Stop();
                return;
            }

            _effect.transform.position = _target.position;
        }

        public bool IsFinished(float minLifetime, float maxLifetime) {
            if (IsAttached)
                return false;

            return _age >= maxLifetime || (_age >= minLifetime && _effect.IsAlive == false);
        }

        public void Deactivate() {
            _target = null;
            IsAttached = false;
            Generation++;
            _effect.gameObject.SetActive(false);
        }
    }
}
