using Features.ActiveRagdollModule;
using Features.TargetModule;
using UnityEngine;
using Zenject;

namespace Features.AttackModule {
    // Decides where an attack lands, on every peer by itself, nothing over the network: wherever the attacker's body is
    // simulated (the owner's own, a copy elsewhere, which attacks when the owner's packets say so), once per attack in
    // its hit window, the attack's limb touching any Hittable target other than the attacker's own (a contact with one
    // of the target's parts, also a target the aim did not pick, so a fingertip on a face always lands), or its reach
    // coming within the attack's hit radius of the aimed target's point (AttackHitRule.DistanceToReach), lands on that
    // target: Target.ReceiveHit, and the target's reactions act on this peer.
    // Every hit and miss is logged as an error for now.
    public sealed class AttackHitDetector : MonoBehaviour {
        [SerializeField] private Attacker _attacker;
        [Tooltip("The ragdoll limb that lands the attack (the slapping hand).")]
        [SerializeField] private RagdollLimb _limb;
        [Tooltip("The attacker's own target, never hit. Empty when it has none.")]
        [SerializeField] private Target _own;

        private TargetRegistry _registry;
        private int _hitAttackNumber;
        private int _missLoggedAttackNumber;
        private int _closestAttackNumber;
        private float _closestThisAttack = float.PositiveInfinity;

        [Inject]
        private void Construct(TargetRegistry registry) =>
            _registry = registry;

        private void OnEnable() {
            if (_limb != null)
                _limb.Touching += HandleLimbTouching;
        }

        private void OnDisable() {
            if (_limb != null)
                _limb.Touching -= HandleLimbTouching;
        }

        private void FixedUpdate() {
            Target target = _attacker.Target;
            if (CanHitNow() == false || target == null || target.IsAvailable == false) {
                LogMissOnce();
                return;
            }

            Vector3 limbPosition = _limb.Rigidbody.position;
            Vector3 limbVelocity = _limb.Rigidbody.linearVelocity;
            Vector3 aimPoint = target.AimPoint.position;
            float distance = AttackHitRule.DistanceToReach(limbPosition, limbVelocity, aimPoint,
                _attacker.Attack.HitReachAhead);
            if (_closestAttackNumber != _attacker.AttackNumber) {
                _closestAttackNumber = _attacker.AttackNumber;
                _closestThisAttack = float.PositiveInfinity;
            }
            _closestThisAttack = Mathf.Min(_closestThisAttack, distance);
            if (distance <= _attacker.Attack.HitRadius)
                Land(target, new TargetHit(_attacker.gameObject, limbVelocity, aimPoint - limbPosition, false));
        }

        // A contact reports the speed the limb had before the touch stopped it.
        private void HandleLimbTouching(RagdollLimb limb, Collision collision) {
            if (CanHitNow() == false || _registry == null)
                return;

            Target target = _registry.FindByPart(TargetKind.Hittable, collision.rigidbody, _own);
            if (target == null)
                return;

            Vector3 toAimPoint = target.AimPoint.position - _limb.Rigidbody.position;
            Land(target, new TargetHit(_attacker.gameObject, toAimPoint.normalized * collision.relativeVelocity.magnitude,
                toAimPoint, true));
        }

        // The limb, simulated on this peer, is in the attack's hit window and has not landed yet this attack.
        private bool CanHitNow() =>
            _limb != null && _limb.Rigidbody.isKinematic == false && _attacker.AttackNumber != _hitAttackNumber
            && _attacker.IsInHitWindow;

        private void Land(Target target, in TargetHit hit) {
            _hitAttackNumber = _attacker.AttackNumber;
            Debug.LogError($"Attack: {name} landed on {target.name} here ({(hit.IsTouch ? "touched" : "came close")}; " +
                $"limb {hit.LimbVelocity.magnitude:F1} m/s, {hit.LimbToAimPoint.magnitude:F2} m from its aim point).", this);
            target.ReceiveHit(hit);
        }

        // Once an attack with a target is past its hit window without landing: how close the limb's reach came.
        private void LogMissOnce() {
            int attack = _attacker.AttackNumber;
            if (_limb == null || _limb.Rigidbody.isKinematic || attack == _hitAttackNumber
                || attack == _missLoggedAttackNumber || _attacker.Target == null || _attacker.IsPastHitWindow == false)
                return;

            _missLoggedAttackNumber = attack;
            string closest = _closestAttackNumber == attack ? $"{_closestThisAttack:F2} m" : "never measured";
            Debug.LogError($"Attack: {name} missed {_attacker.Target.name} here: the limb's reach came {closest} from " +
                $"its aim point (hit radius {_attacker.Attack.HitRadius:F2} m, " +
                $"{_attacker.Attack.HitReachAhead:F2} m ahead of the limb, no touch).", this);
        }
    }
}
