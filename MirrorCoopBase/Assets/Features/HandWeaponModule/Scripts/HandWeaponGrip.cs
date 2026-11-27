using Features.PlayerAnimationModule.Reach;
using Features.WeaponModule.Scripts.Parts.Holding;
using UnityEngine;

namespace Features.HandWeaponModule.Scripts {
    // The player's real right hand as the place a hand weapon sits, on every peer that simulates the body. While a
    // weapon is held the arm reaches along the aim (a LimbReach request below an attack's, so a slap still takes the
    // hand), and the weapon is placed with its grip in the palm (GripPoint). The same pose on every peer and in both
    // views, since it is the body's own hand.
    public sealed class HandWeaponGrip : MonoBehaviour {
        // An attack (the slap, priority 0 and newer) wins the hand over the weapon.
        private const int REACH_PRIORITY = -1;

        [SerializeField] private LimbReach _reach;
        [SerializeField] private Transform _upperArm;
        [SerializeField] private Transform _lowerArm;
        [SerializeField] private Transform _hand;
        [Tooltip("A bone toward the fingers (the middle finger's first bone): the palm lies between it and the wrist.")]
        [SerializeField] private Transform _fingers;
        [Tooltip("Where the palm is between the wrist (0) and the finger bone (1).")]
        [SerializeField, Range(0f, 1f)] private float _palmShare = 0.5f;
        [Tooltip("How far the hand reaches along the aim from the shoulder, as a share of the arm's length (1: straight).")]
        [SerializeField, Range(0.3f, 1f)] private float _reachShare = 0.85f;

        private LimbReachRequest _request;
        private IWeaponAim _aim;
        private float _armLength;

        public Vector3 GripPoint => Vector3.Lerp(_hand.position, _fingers.position, _palmShare);
        public bool IsValid => _reach != null && _upperArm != null && _lowerArm != null && _hand != null && _fingers != null;

        private void Awake() {
            if (IsValid)
                _armLength = Vector3.Distance(_upperArm.position, _lowerArm.position) + Vector3.Distance(_lowerArm.position, _hand.position);
        }

        private void OnDisable() =>
            End();

        public void Begin(IWeaponAim aim) {
            End();
            _aim = aim;
            if (IsValid)
                _request = _reach.Request(LimbId.RightHand, REACH_PRIORITY);
        }

        public void End() {
            _request?.Release();
            _request = null;
            _aim = null;
        }

        // Runs before the ragdoll samples its pose (LimbReach reads the goal on that sample).
        private void FixedUpdate() {
            if (_request == null)
                return;

            Vector3 direction = _aim.Direction;
            if (direction.sqrMagnitude < 1e-6f) {
                _request.Weight = 0f;
                return;
            }

            _request.Weight = 1f;
            _request.SetGoal(_upperArm.position + direction.normalized * (_armLength * _reachShare));
        }
    }
}
