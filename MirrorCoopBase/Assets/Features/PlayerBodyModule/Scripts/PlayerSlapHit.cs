using Features.ActiveRagdollModule;
using Features.AttackModule;
using Features.CharacterMovableModule.Scripts;
using Features.PlayerReplicaModule;
using UnityEngine;
using Zenject;

namespace Features.PlayerBodyModule {
    // The slap landing on a head, played on every peer by itself, nothing over the network: wherever the slapper's body
    // is simulated (the owner's own, a copy elsewhere, which attacks when the owner's packets say so), the hand of that
    // body touching any other player's head (a contact with any collider of that head, the face too; also a head the
    // aim did not pick, so a fingertip on a face always lands), or its reach coming close enough to the centre of the
    // head its Attacker aims at, once per attack in the attack's hit window, pushes the slapped
    // player's head limb and a share of it the chest on this peer. So the push comes exactly when the hand reaches the
    // head as this peer shows it. The slapped player's owner pushes its real body, the others follow its packets; a peer
    // where the hand misses shows no push. A head alone is too light: its push spreads over the whole body and only
    // sways it.
    // It is also this player's head as a target of other players' attacks (PlayerAttackAim), until the TargetModule.
    // Every hit and miss is logged as an error for now.
    public sealed class PlayerSlapHit : MonoBehaviour {
        [SerializeField] private Attacker _attacker;
        [Tooltip("The attack's aim: which player's head the attack went for.")]
        [SerializeField] private PlayerAttackAim _aim;
        [SerializeField] private CharacterMovableBase _movable;
        [Tooltip("This player's own head: what other players' slaps aim at and push.")]
        [SerializeField] private RagdollLimb _head;
        [Tooltip("A point on the head bone in the middle of the head: where slaps aim and land. The head limb's own pivot is down at the neck.")]
        [SerializeField] private Transform _headCenter;
        [Tooltip("The ragdoll limb of the slapping hand.")]
        [SerializeField] private RagdollLimb _hand;
        [Tooltip("This player's chest limb: a slap on this player's head pushes it too, so the upper body bends.")]
        [SerializeField] private RagdollLimb _chest;

        [Header("Hit")]
        [Tooltip("Besides a touch, the hand hits when its centre comes this close to the head's centre, metres. The head is big (its collider is about 0.4 m round), so a touching hand stays about 0.5 m from the centre.")]
        [SerializeField, Min(0f)] private float _hitRadius = 0.7f;
        [Tooltip("The hit zone reaches this far ahead of the hand along its motion, metres: a hand swinging toward the head hits it a little earlier, so slaps land more easily.")]
        [SerializeField, Min(0f)] private float _hitReachAhead = 0.6f;
        [Tooltip("The head's velocity change is the hand's velocity times this.")]
        [SerializeField, Min(0f)] private float _handSpeedScale = 6f;
        [Tooltip("The head's velocity change is at least this, m/s, however slowly the hand moves.")]
        [SerializeField, Min(0f)] private float _minHeadSpeed = 14f;
        [Tooltip("The largest velocity change a hit gives the head, m/s.")]
        [SerializeField, Min(0f)] private float _maxHeadSpeed = 40f;
        [Tooltip("The chest's velocity change is the head's times this.")]
        [SerializeField, Range(0f, 1f)] private float _chestShare = 0.6f;

        private int _hitAttackNumber;
        private int _missLoggedAttackNumber;
        private float _closestThisAttack = float.PositiveInfinity;
        private int _closestAttackNumber;
        private PlayerSlapHit _target;
        private BodyStateChannelRegistry _registry;

        public Transform HeadCenter => _headCenter != null ? _headCenter : _head.transform;
        // A sleeping ragdoll (a dead player, a body nobody simulates here) has kinematic limbs: nothing to aim at.
        public bool IsAwake => _head != null && _head.Rigidbody.isKinematic == false;

        [Inject]
        private void Construct(BodyStateChannelRegistry registry) =>
            _registry = registry;

        private void OnEnable() {
            _attacker.Started += HandleAttackStarted;
            if (_hand != null)
                _hand.Touching += HandleHandTouching;
        }

        private void OnDisable() {
            _attacker.Started -= HandleAttackStarted;
            if (_hand != null)
                _hand.Touching -= HandleHandTouching;
            _target = null;
        }

        // The aim picked the target just before the attack started.
        private void HandleAttackStarted(Attacker attacker) =>
            _target = _aim.PickedHead;

        private void FixedUpdate() {
            if (TryGetTarget(out PlayerSlapHit target) == false) {
                LogMissOnce();
                return;
            }

            Vector3 handPosition = _hand.Rigidbody.position;
            Vector3 handVelocity = _hand.Rigidbody.linearVelocity;
            Vector3 toHead = target.HeadCenter.position - handPosition;
            float distance = SlapHitRule.DistanceToReach(handPosition, handVelocity, target.HeadCenter.position,
                _hitReachAhead);
            if (_closestAttackNumber != _attacker.AttackNumber) {
                _closestAttackNumber = _attacker.AttackNumber;
                _closestThisAttack = float.PositiveInfinity;
            }
            _closestThisAttack = Mathf.Min(_closestThisAttack, distance);
            if (distance <= _hitRadius)
                Hit(target, toHead, handVelocity, "came close");
        }

        // A contact reports the speed the hand had before the touch stopped it. Any other player's head counts, aimed at
        // or not.
        private void HandleHandTouching(RagdollLimb hand, Collision collision) {
            if (CanHitNow() == false || collision.rigidbody == null)
                return;

            PlayerSlapHit target = FindHeadOf(collision.rigidbody);
            if (target == null)
                return;

            Vector3 toHead = target.HeadCenter.position - _hand.Rigidbody.position;
            Hit(target, toHead, toHead.normalized * collision.relativeVelocity.magnitude, "touched");
        }

        // The attack's target while this body's hand, simulated on this peer, may still hit it this attack.
        private bool TryGetTarget(out PlayerSlapHit target) {
            target = _target;
            return CanHitNow() && target != null && target._head != null;
        }

        // This body's hand, simulated on this peer, is in the attack's hit window and has not hit yet this attack.
        private bool CanHitNow() =>
            _hand != null && _hand.Rigidbody.isKinematic == false && _attacker.AttackNumber != _hitAttackNumber
            && _attacker.IsInHitWindow;

        // The other player whose head limb has this rigidbody; null for anything else (a chest, a wall, its own head).
        private PlayerSlapHit FindHeadOf(Rigidbody body) {
            if (_registry == null)
                return null;

            foreach (BodyStateChannel channel in _registry.Channels) {
                if (channel == null || channel.gameObject == gameObject)
                    continue;
                if (channel.TryGetComponent(out PlayerSlapHit other) && other._head != null && other._head.Rigidbody == body)
                    return other;
            }

            return null;
        }

        private void Hit(PlayerSlapHit target, Vector3 toHead, Vector3 handVelocity, string how) {
            _hitAttackNumber = _attacker.AttackNumber;
            Vector3 headVelocity = Vector3.ClampMagnitude(
                SlapHitRule.HeadVelocity(handVelocity, toHead, _handSpeedScale, _minHeadSpeed), _maxHeadSpeed);
            Debug.LogError($"Slap: {name} hit {target.name}'s head here ({how}; hand {handVelocity.magnitude:F1} m/s, " +
                $"head gets {headVelocity.magnitude:F1} m/s, {toHead.magnitude:F2} m from the head's centre).", this);
            target.TakeHit(headVelocity, _chestShare);
        }

        // Jerks this player's head at its centre (above the limb's pivot at the neck, so the head also tips away) and
        // pushes the chest by a share of it, on the body this peer simulates. A held (seated) player and a sleeping
        // ragdoll are left alone.
        private void TakeHit(Vector3 headVelocity, float chestShare) {
            if (_movable.IsHeld || IsAwake == false) {
                Debug.LogError($"Slap: {name} was hit but not pushed here (held: {_movable.IsHeld}, " +
                    $"head asleep: {IsAwake == false}).", this);
                return;
            }

            _head.Rigidbody.AddForceAtPosition(headVelocity, HeadCenter.position, ForceMode.VelocityChange);
            if (_chest != null && _chest.Rigidbody.isKinematic == false)
                _chest.Rigidbody.AddForce(headVelocity * chestShare, ForceMode.VelocityChange);
        }

        // Once an attack with a target is past its hit window without a hit: how close the hand came.
        private void LogMissOnce() {
            int attack = _attacker.AttackNumber;
            if (_hand == null || _hand.Rigidbody.isKinematic || attack == _hitAttackNumber
                || attack == _missLoggedAttackNumber || _target == null || _attacker.IsPastHitWindow == false)
                return;

            _missLoggedAttackNumber = attack;
            string closest = _closestAttackNumber == attack ? $"{_closestThisAttack:F2} m" : "never measured";
            Debug.LogError($"Slap: {name} missed {_target.name}'s head here: the hand's reach came {closest} from " +
                $"its centre (hit radius {_hitRadius:F2} m, {_hitReachAhead:F2} m ahead of the hand, no touch).", this);
        }
    }
}
