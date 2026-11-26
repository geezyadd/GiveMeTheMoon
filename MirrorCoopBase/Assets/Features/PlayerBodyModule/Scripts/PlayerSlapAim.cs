using System.Collections.Generic;
using Features.ActiveRagdollModule;
using Features.CharacterMovableModule.Scripts;
using Features.PlayerAnimationModule;
using Features.PlayerAnimationModule.Reach;
using Features.PlayerReplicaModule;
using UnityEngine;
using Zenject;

namespace Features.PlayerBodyModule {
    // Points the slapping arm at a head. On every peer that simulates the body, when a slap starts, it picks the
    // nearest head of another player in front (SlapAimRule.PickHead); during the slap the right hand reaches for that
    // head through the body's LimbReach with a weight that eases in and out (SlapAimRule.Weight). No head in front: the
    // weight stays 0 and the slap is the plain animation. Runs before the ragdoll samples its pose in the same step.
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerSlapAim : MonoBehaviour {
        private const int REACH_PRIORITY = 0;


        [SerializeField] private PlayerAnimationDriver _animation;
        [SerializeField] private CharacterMovableBase _movable;
        [Tooltip("The ragdoll's limb reach: the right hand reaches for the aimed head.")]
        [SerializeField] private LimbReach _reach;
        [Tooltip("This player's own head: where the aim is measured from and what other players' slaps aim at.")]
        [SerializeField] private RagdollLimb _head;
        [Tooltip("A point on the head bone in the middle of the head: where slaps aim and land. The head limb's own pivot is down at the neck.")]
        [SerializeField] private Transform _headCenter;

        [Header("Picking a head")]
        [Tooltip("The farthest head the slap goes for, from the slapper's own head, metres.")]
        [SerializeField, Min(0f)] private float _maxDistance = 1.3f;
        [Tooltip("How far to either side of the slapper's facing a head may be, degrees.")]
        [SerializeField, Range(0f, 180f)] private float _maxAngle = 60f;
        [Tooltip("The hand aims this far past the head, along the line from the slapper's head through it, metres: it swings through the head at speed instead of slowing down on it.")]
        [SerializeField, Min(0f)] private float _reachThrough = 0.3f;

        [Header("Reach over the slap (seconds since its start)")]
        [Tooltip("Until this time the slap is the plain animation (its wind-up); then the arm starts reaching for the head.")]
        [SerializeField, Min(0f)] private float _startAtSeconds = 0.2f;
        [Tooltip("The arm reaches fully for the head by this time.")]
        [SerializeField, Min(0f)] private float _fullAtSeconds = 0.34f;
        [Tooltip("The arm keeps reaching fully until this time.")]
        [SerializeField, Min(0f)] private float _releaseAtSeconds = 0.5f;
        [Tooltip("The arm is back to the plain animation by this time.")]
        [SerializeField, Min(0f)] private float _endAtSeconds = 0.8f;

        private readonly List<PlayerSlapAim> _candidates = new();
        private readonly List<Vector3> _candidateHeads = new();

        private BodyStateChannelRegistry _registry;
        private LimbReachRequest _handReach;
        private float _slapStartedAt = float.NegativeInfinity;

        public RagdollLimb Head => _head;
        public Vector3 HeadCenter => _headCenter != null ? _headCenter.position : _head.Rigidbody.position;
        // The player whose head this slap goes for; null when there was none in front.
        public PlayerSlapAim AimedTarget { get; private set; }
        // Goes up by one with each slap.
        public int SlapNumber { get; private set; }
        public float SecondsSinceSlap => Time.time - _slapStartedAt;

        [Inject]
        private void Construct(BodyStateChannelRegistry registry) =>
            _registry = registry;

        private void OnEnable() {
            _animation.Slapped += HandleSlapped;
            _handReach = _reach.Request(LimbId.RightHand, REACH_PRIORITY);
        }

        private void OnDisable() {
            _animation.Slapped -= HandleSlapped;
            _handReach?.Release();
            _handReach = null;
            AimedTarget = null;
        }

        private void HandleSlapped() {
            _slapStartedAt = Time.time;
            SlapNumber++;
            AimedTarget = PickTarget();
            LogAim();
        }

        // For now every slap's aim is logged as an error, on every peer that simulates the slapper.
        private void LogAim() {
            if (AimedTarget != null) {
                Debug.LogError($"Slap: {name} aims at {AimedTarget.name}'s head, " +
                    $"{Vector3.Distance(HeadCenter, AimedTarget.HeadCenter):F2} m away.", this);
                return;
            }

            string others = _candidateHeads.Count == 0 ? "none awake here" : "";
            for (int i = 0; i < _candidateHeads.Count; i++) {
                Vector3 offset = _candidateHeads[i] - HeadCenter;
                Vector3 flat = new(offset.x, 0f, offset.z);
                float angle = Vector3.Angle(Quaternion.Euler(0f, _movable.ShownLookYaw, 0f) * Vector3.forward, flat);
                others += $"{(i > 0 ? "; " : "")}{_candidates[i].name} at {offset.magnitude:F2} m, {angle:F0}° off the look";
            }
            Debug.LogError($"Slap: {name} found no head in front (limit {_maxDistance:F2} m, {_maxAngle:F0}°; " +
                $"other heads: {others}).", this);
        }

        // Every step before the sample: the hand's goal stands past the aimed head.
        private void FixedUpdate() {
            float weight = IsUsable(AimedTarget)
                ? SlapAimRule.Weight(SecondsSinceSlap, _startAtSeconds, _fullAtSeconds, _releaseAtSeconds, _endAtSeconds)
                : 0f;
            if (weight > 0f)
                _handReach.SetGoal(ReachPoint(AimedTarget.HeadCenter));
            else if (SecondsSinceSlap >= _endAtSeconds)
                AimedTarget = null;

            _handReach.Weight = weight;
        }

        private Vector3 ReachPoint(Vector3 targetHead) {
            Vector3 through = targetHead - HeadCenter;
            return through.sqrMagnitude > 0.0001f ? targetHead + through.normalized * _reachThrough : targetHead;
        }

        private PlayerSlapAim PickTarget() {
            if (_registry == null || _head == null)
                return null;

            _candidates.Clear();
            _candidateHeads.Clear();
            foreach (BodyStateChannel channel in _registry.Channels) {
                if (channel == null || channel.gameObject == gameObject)
                    continue;
                if (channel.TryGetComponent(out PlayerSlapAim other) == false || IsUsable(other) == false)
                    continue;

                _candidates.Add(other);
                _candidateHeads.Add(other.HeadCenter);
            }

            int picked = SlapAimRule.PickHead(HeadCenter, _movable.ShownLookYaw, _candidateHeads,
                _maxDistance, _maxAngle);
            return picked >= 0 ? _candidates[picked] : null;
        }

        // A sleeping ragdoll (a dead player, a body nobody simulates here) has kinematic limbs: nothing to aim at.
        private static bool IsUsable(PlayerSlapAim target) =>
            target != null && target._head != null && target._head.Rigidbody.isKinematic == false;
    }
}
