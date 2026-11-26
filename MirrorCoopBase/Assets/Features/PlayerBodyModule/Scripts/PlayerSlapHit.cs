using Features.ActiveRagdollModule;
using Features.CharacterMovableModule.Scripts;
using UnityEngine;

namespace Features.PlayerBodyModule {
    // The slap landing on a head, played on every peer by itself, nothing over the network: wherever the slapper's body
    // is simulated (the owner's own, a copy elsewhere, which slaps when the owner's packets say so), the hand of that
    // body touching the head its PlayerSlapAim aims at (a contact with any collider of that head, the face too), or its
    // centre coming close enough to the head's centre, once per slap in the hit window, pushes the slapped player's head
    // limb and a share of it the chest on this peer. So the push comes exactly when the hand reaches the head as this
    // peer shows it. The slapped player's owner pushes its real body, the others follow its packets; a peer where the
    // hand misses shows no push. A head alone is too light: its push spreads over the whole body and only sways it.
    // Every hit and miss is logged as an error for now.
    public sealed class PlayerSlapHit : MonoBehaviour {
        [SerializeField] private PlayerSlapAim _aim;
        [SerializeField] private CharacterMovableBase _movable;
        [Tooltip("The ragdoll limb of the slapping hand.")]
        [SerializeField] private RagdollLimb _hand;
        [Tooltip("This player's chest limb: a slap on this player's head pushes it too, so the upper body bends.")]
        [SerializeField] private RagdollLimb _chest;

        [Header("Hit window (seconds since the slap's start)")]
        [SerializeField, Min(0f)] private float _hitStartSeconds = 0.15f;
        [SerializeField, Min(0f)] private float _hitEndSeconds = 0.6f;

        [Header("Hit")]
        [Tooltip("Besides a touch, the hand hits when its centre comes this close to the head's centre, metres. The head is big (its collider is about 0.4 m round), so a touching hand stays about 0.5 m from the centre.")]
        [SerializeField, Min(0f)] private float _hitRadius = 0.6f;
        [Tooltip("The head's velocity change is the hand's velocity times this.")]
        [SerializeField, Min(0f)] private float _handSpeedScale = 6f;
        [Tooltip("The head's velocity change is at least this, m/s, however slowly the hand moves.")]
        [SerializeField, Min(0f)] private float _minHeadSpeed = 14f;
        [Tooltip("The largest velocity change a hit gives the head, m/s.")]
        [SerializeField, Min(0f)] private float _maxHeadSpeed = 40f;
        [Tooltip("The chest's velocity change is the head's times this.")]
        [SerializeField, Range(0f, 1f)] private float _chestShare = 0.6f;

        private int _hitSlapNumber;
        private int _missLoggedSlapNumber;
        private float _closestThisSlap = float.PositiveInfinity;
        private int _closestSlapNumber;

        private void OnEnable() {
            if (_hand != null)
                _hand.Touching += HandleHandTouching;
        }

        private void OnDisable() {
            if (_hand != null)
                _hand.Touching -= HandleHandTouching;
        }

        private void FixedUpdate() {
            if (TryGetTarget(out PlayerSlapAim target) == false) {
                LogMissOnce();
                return;
            }

            Vector3 toHead = target.HeadCenter - _hand.Rigidbody.position;
            float distance = toHead.magnitude;
            if (_closestSlapNumber != _aim.SlapNumber) {
                _closestSlapNumber = _aim.SlapNumber;
                _closestThisSlap = float.PositiveInfinity;
            }
            _closestThisSlap = Mathf.Min(_closestThisSlap, distance);
            if (distance <= _hitRadius)
                Hit(target, toHead, _hand.Rigidbody.linearVelocity, "came close");
        }

        // A contact reports the speed the hand had before the touch stopped it.
        private void HandleHandTouching(RagdollLimb hand, Collision collision) {
            if (TryGetTarget(out PlayerSlapAim target) == false || collision.rigidbody != target.Head.Rigidbody)
                return;

            Vector3 toHead = target.HeadCenter - _hand.Rigidbody.position;
            Hit(target, toHead, toHead.normalized * collision.relativeVelocity.magnitude, "touched");
        }

        // The slap's target while this body's hand, simulated on this peer, may still hit it this slap.
        private bool TryGetTarget(out PlayerSlapAim target) {
            target = null;
            if (_hand == null || _hand.Rigidbody.isKinematic || _aim.SlapNumber == _hitSlapNumber)
                return false;

            float sinceSlap = _aim.SecondsSinceSlap;
            target = _aim.AimedTarget;
            return target != null && target.Head != null && sinceSlap >= _hitStartSeconds && sinceSlap <= _hitEndSeconds;
        }

        private void Hit(PlayerSlapAim target, Vector3 toHead, Vector3 handVelocity, string how) {
            if (target.TryGetComponent(out PlayerSlapHit slapped) == false)
                return;

            _hitSlapNumber = _aim.SlapNumber;
            Vector3 headVelocity = Vector3.ClampMagnitude(
                SlapHitRule.HeadVelocity(handVelocity, toHead, _handSpeedScale, _minHeadSpeed), _maxHeadSpeed);
            Debug.LogError($"Slap: {name} hit {target.name}'s head here ({how}; hand {handVelocity.magnitude:F1} m/s, " +
                $"head gets {headVelocity.magnitude:F1} m/s, {toHead.magnitude:F2} m from the head's centre).", this);
            slapped.TakeHit(headVelocity, _chestShare);
        }

        // Jerks this player's head at its centre (above the limb's pivot at the neck, so the head also tips away) and
        // pushes the chest by a share of it, on the body this peer simulates. A held (seated) player and a sleeping
        // ragdoll are left alone.
        private void TakeHit(Vector3 headVelocity, float chestShare) {
            RagdollLimb head = _aim.Head;
            if (_movable.IsHeld || head == null || head.Rigidbody.isKinematic) {
                Debug.LogError($"Slap: {name} was hit but not pushed here (held: {_movable.IsHeld}, " +
                    $"head asleep: {head == null || head.Rigidbody.isKinematic}).", this);
                return;
            }

            head.Rigidbody.AddForceAtPosition(headVelocity, _aim.HeadCenter, ForceMode.VelocityChange);
            if (_chest != null && _chest.Rigidbody.isKinematic == false)
                _chest.Rigidbody.AddForce(headVelocity * chestShare, ForceMode.VelocityChange);
        }

        // Once a slap with a target is over without a hit: how close the hand came.
        private void LogMissOnce() {
            int slap = _aim.SlapNumber;
            if (_hand == null || _hand.Rigidbody.isKinematic || slap == _hitSlapNumber || slap == _missLoggedSlapNumber
                || _aim.AimedTarget == null || _aim.SecondsSinceSlap <= _hitEndSeconds)
                return;

            _missLoggedSlapNumber = slap;
            string closest = _closestSlapNumber == slap ? $"{_closestThisSlap:F2} m" : "never measured";
            Debug.LogError($"Slap: {name} missed {_aim.AimedTarget.name}'s head here: the hand came {closest} from " +
                $"its centre (hit radius {_hitRadius:F2} m, no touch).", this);
        }
    }
}
