using System;
using Features.AttackModule.Contracts;
using Features.PlayerAnimationModule;
using Features.PlayerAnimationModule.Reach;
using UnityEngine;

namespace Features.AttackModule {
    // Carries out a body's attacks on every peer that simulates the body, from the presses it reads there
    // (IAttackPresses: the owner's button, a copy's packets). A press starts the attack when none is running and the
    // body can animate: the attack's animator trigger fires, the target is picked once (IAttackAim), and the attack's
    // limb reaches for it through the body's LimbReach as AttackTimeline says. A press during an attack is ignored.
    // Hits and their effects are not decided here: others read the attack's state (Target, IsInHitWindow) and events.
    // Runs before the ragdoll samples its pose in the same physics step.
    [DefaultExecutionOrder(-100)]
    public sealed class Attacker : MonoBehaviour {
        [SerializeField] private AttackDefinition _attack;
        [Tooltip("Where the presses come from on this peer: a component that implements IAttackPresses.")]
        [SerializeField] private MonoBehaviour _presses;
        [Tooltip("What picks the target when an attack starts: a component that implements IAttackAim.")]
        [SerializeField] private MonoBehaviour _aim;
        [SerializeField] private PlayerAnimationDriver _animation;
        [SerializeField] private LimbReach _reach;
        [Tooltip("The attacker's own point the limb aims from, through the target (the middle of its head).")]
        [SerializeField] private Transform _aimOrigin;

        private readonly AttackPressCounter _pressCounter = new();

        private IAttackPresses _pressSource;
        private IAttackAim _aimSource;
        private LimbReachRequest _limbReach;
        private float _startedAt = float.NegativeInfinity;

        // When an attack starts and when it is over, on every peer that simulates the attacker.
        public event Action<Attacker> Started;
        public event Action<Attacker> Ended;

        public AttackDefinition Attack => _attack;
        public bool IsAttacking { get; private set; }
        // Goes up by one with each attack.
        public int AttackNumber { get; private set; }
        // What the last attack went for; null when there was nothing in reach. Kept after the attack is over.
        public Transform Target { get; private set; }
        public float SecondsSinceStart => Time.time - _startedAt;
        public bool IsInHitWindow => IsAttacking && AttackTimeline.IsInHitWindow(_attack.Timing, SecondsSinceStart);
        public bool IsPastHitWindow => AttackTimeline.IsPastHitWindow(_attack.Timing, SecondsSinceStart);

        private void Awake() {
            _pressSource = _presses as IAttackPresses;
            _aimSource = _aim as IAttackAim;
            if (_pressSource == null || _aimSource == null)
                Debug.LogError($"Attack: {name} needs an IAttackPresses and an IAttackAim; it never attacks.", this);
        }

        private void OnEnable() {
            _pressCounter.Reset();
            _limbReach = _reach.Request(_attack.Limb, _attack.ReachPriority);
        }

        private void OnDisable() {
            _limbReach?.Release();
            _limbReach = null;
            Finish();
        }

        private void FixedUpdate() {
            bool isPressed = _pressSource != null && _pressCounter.Step(_pressSource.IsAvailable, _pressSource.AttackCount);
            if (IsAttacking && AttackTimeline.IsOver(_attack.Timing, SecondsSinceStart))
                Finish();
            if (isPressed && IsAttacking == false && _animation.CanPlay)
                Begin();

            ReachForTarget();
        }

        private void Begin() {
            _startedAt = Time.time;
            AttackNumber++;
            IsAttacking = true;
            Target = _aimSource.PickTarget(_attack);
            _animation.Play(_attack.AnimatorTriggerHash);
            Started?.Invoke(this);
        }

        private void Finish() {
            if (IsAttacking == false)
                return;

            IsAttacking = false;
            Ended?.Invoke(this);
        }

        // Every step before the sample: the limb's goal stands past the target.
        private void ReachForTarget() {
            float weight = IsAttacking && Target != null
                ? AttackTimeline.ReachWeight(_attack.Timing, SecondsSinceStart)
                : 0f;
            if (weight > 0f)
                _limbReach.SetGoal(ReachPoint(Target.position));

            _limbReach.Weight = weight;
        }

        private Vector3 ReachPoint(Vector3 target) {
            Vector3 through = target - _aimOrigin.position;
            return through.sqrMagnitude > 0.0001f ? target + through.normalized * _attack.ReachThrough : target;
        }
    }
}
