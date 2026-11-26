using Features.ActiveRagdollModule;
using Features.CharacterMovableModule.Scripts;
using Features.TargetModule;
using UnityEngine;

namespace Features.PlayerBodyModule {
    // A player's reaction to a hit on its head target: on the peer where the hit landed, the head limb gets a velocity
    // change at the target's aim point (above the limb's pivot at the neck, so the head also tips away) and the chest a
    // share of it: the head jerks, the upper body bends at the hips and the joints bring it back. The slapped player's
    // owner pushes its real body, the others follow its packets. A held (seated) player and a sleeping ragdoll are left
    // alone. A head alone is too light: its push spreads over the whole body and only sways it.
    public sealed class PlayerHitPush : MonoBehaviour {
        [SerializeField] private Target _target;
        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private RagdollLimb _head;
        [Tooltip("This player's chest limb: a hit on the head pushes it too, so the upper body bends.")]
        [SerializeField] private RagdollLimb _chest;

        [Tooltip("The head's velocity change is the hitting limb's velocity times this.")]
        [SerializeField, Min(0f)] private float _limbSpeedScale = 6f;
        [Tooltip("The head's velocity change is at least this, m/s, however slowly the limb moves.")]
        [SerializeField, Min(0f)] private float _minHeadSpeed = 14f;
        [Tooltip("The largest velocity change a hit gives the head, m/s.")]
        [SerializeField, Min(0f)] private float _maxHeadSpeed = 40f;
        [Tooltip("The chest's velocity change is the head's times this.")]
        [SerializeField, Range(0f, 1f)] private float _chestShare = 0.6f;

        private void OnEnable() =>
            _target.Hit += HandleHit;

        private void OnDisable() =>
            _target.Hit -= HandleHit;

        private void HandleHit(TargetHit hit) {
            if (_movable.IsHeld || _head == null || _head.Rigidbody.isKinematic)
                return;

            Vector3 headVelocity = Vector3.ClampMagnitude(
                HitPushRule.HeadVelocity(hit.LimbVelocity, hit.LimbToAimPoint, _limbSpeedScale, _minHeadSpeed),
                _maxHeadSpeed);
            _head.Rigidbody.AddForceAtPosition(headVelocity, _target.AimPoint.position, ForceMode.VelocityChange);
            if (_chest != null && _chest.Rigidbody.isKinematic == false)
                _chest.Rigidbody.AddForce(headVelocity * _chestShare, ForceMode.VelocityChange);
        }
    }
}
