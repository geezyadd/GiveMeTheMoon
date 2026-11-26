using Features.ExplosionModule.Configuration;
using Features.TargetModule;
using UnityEngine;
using Zenject;

namespace Features.ExplosionModule.Testing {
    // A test object for the lobby: on the server, when a player (a hittable target that is simulated, so not dead)
    // comes near, it sets off its explosion and waits for its cooldown before it can go off again.
    public sealed class ExplosionTestMine : MonoBehaviour {
        [SerializeField] private ExplosionPreset _preset;
        [Tooltip("Goes off when a player's head is this close to the mine on the ground plane, m.")]
        [SerializeField, Min(0.1f)] private float _triggerRadius = 2f;
        [Tooltip("...and at most this high above the mine, m (the scaled-up player's head is about 4 m up).")]
        [SerializeField, Min(0.1f)] private float _triggerHeight = 6f;
        [Tooltip("Seconds after going off before it can go off again.")]
        [SerializeField, Min(0f)] private float _cooldownSeconds = 2.5f;
        [Tooltip("The explosion's centre, above the mine's pivot, m.")]
        [SerializeField] private float _centerHeight = 0.3f;

        [Inject] private IExplosionService _explosionService;
        [Inject] private TargetRegistry _targetRegistry;

        private float _cooldownLeft;

        private void FixedUpdate() {
            if (Mirror.NetworkServer.active == false || _explosionService == null || _preset == null)
                return;

            if (_cooldownLeft > 0f) {
                _cooldownLeft -= Time.fixedDeltaTime;
                return;
            }

            if (IsPlayerNear() == false)
                return;

            _explosionService.ServerExplode(transform.position + Vector3.up * _centerHeight, _preset);
            _cooldownLeft = _cooldownSeconds;
        }

        private bool IsPlayerNear() {
            float radiusSquared = _triggerRadius * _triggerRadius;
            foreach (Target target in _targetRegistry.Targets) {
                if (target == null || target.Is(TargetKind.Hittable) == false || target.IsAvailable == false)
                    continue;

                Vector3 offset = target.AimPoint.position - transform.position;
                float height = offset.y;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radiusSquared && height >= 0f && height <= _triggerHeight)
                    return true;
            }

            return false;
        }
    }
}
