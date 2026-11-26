using Features.PlayerAnimationModule.Reach;
using UnityEngine;

namespace Features.AttackModule {
    // One kind of attack (a slap, later a kick or a headbutt): the animation it plays, the limb that reaches for the
    // target, its timing and how far it looks for a target. Any attacker can use it.
    [CreateAssetMenu(fileName = "Attack", menuName = "Attacks/" + nameof(AttackDefinition))]
    public sealed class AttackDefinition : ScriptableObject {
        [Header("Animation")]
        [Tooltip("The animator trigger that starts the attack's animation (the attacker's animator must have it).")]
        [SerializeField] private string _animatorTrigger = "Slap";

        [Header("Limb")]
        [Tooltip("The limb that reaches for the target during the attack.")]
        [SerializeField] private LimbId _limb = LimbId.RightHand;
        [Tooltip("The limb follows the request of the highest priority; an attack over a hand on an item wins with a higher one.")]
        [SerializeField] private int _reachPriority;

        [Header("Timing (seconds since the attack's start)")]
        [SerializeField] private AttackTiming _timing = new(0.2f, 0.34f, 0.5f, 0.8f, 0.15f, 0.6f, 0.8f);

        [Header("Target")]
        [Tooltip("The farthest target the attack goes for, from the attacker's aim origin, metres.")]
        [SerializeField, Min(0f)] private float _maxDistance = 1.3f;
        [Tooltip("How far to either side of the attacker's facing a target may be, degrees.")]
        [SerializeField, Range(0f, 180f)] private float _maxAngle = 60f;
        [Tooltip("The limb aims this far past the target, along the line from the aim origin through it, metres: it swings through the target at speed instead of slowing down on it.")]
        [SerializeField, Min(0f)] private float _reachThrough = 0.3f;

        private int _animatorTriggerHash;
        private string _hashedTrigger;

        public LimbId Limb => _limb;
        public int ReachPriority => _reachPriority;
        public AttackTiming Timing => _timing;
        public float MaxDistance => _maxDistance;
        public float MaxAngle => _maxAngle;
        public float ReachThrough => _reachThrough;

        public int AnimatorTriggerHash {
            get {
                if (_hashedTrigger != _animatorTrigger) {
                    _hashedTrigger = _animatorTrigger;
                    _animatorTriggerHash = Animator.StringToHash(_animatorTrigger);
                }

                return _animatorTriggerHash;
            }
        }
    }
}
