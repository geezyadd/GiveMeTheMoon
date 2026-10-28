using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Client;
using UnityEngine;

namespace Features.BugModule.Scripts.View {
    // The root of the bug look: drives its parts from BugViewMotion every shown frame. The frame (everything under the
    // field agent body) nods and sways while the bug sits on the ship and squashes when struck; the wings beat, the
    // legs swing and the tail's base twitches, and the tail's bone chain trails behind on its Animation Rigging rig
    // (Damped Transform per bone on the prefab). A killed bug's look vanishes until the pool takes it back.
    public sealed class BugBodyView : MonoBehaviour, IFieldAgentViewLifetime, IFieldAgentViewMotion {
        // Spreads the motion of neighbouring bugs (the golden ratio never repeats a phase).
        private const float PHASE_PER_ID = 0.618034f;
        // The right wing beats as the mirror of the left one.
        private const float MIRROR = -1f;

        [SerializeField] private BugViewFeel _feel;
        [SerializeField] private Transform _frame;
        [SerializeField] private Transform _tailBase;
        [SerializeField] private Transform _leftWing;
        [SerializeField] private Transform _rightWing;
        [SerializeField] private Transform[] _legs;
        [SerializeField] private BugTargetCollider _targetCollider;

        private BugViewMotion _motion;
        private Quaternion _tailBaseRest;
        private Quaternion _leftWingRest;
        private Quaternion _rightWingRest;
        private Quaternion[] _legRests;
        private float _phase;
        private float _flinchSecondsLeft;
        private bool _isDead;

        private void Awake() {
            _motion = new BugViewMotion(_feel);
            _tailBaseRest = _tailBase.localRotation;
            _leftWingRest = _leftWing.localRotation;
            _rightWingRest = _rightWing.localRotation;
            _legRests = new Quaternion[_legs.Length];
            for (int i = 0; i < _legs.Length; i++)
                _legRests[i] = _legs[i].localRotation;
        }

        public void Begin(int agentId) {
            _phase = agentId * PHASE_PER_ID;
            ResetLook();
        }

        public void End() =>
            ResetLook();

        public void Show(FieldAgentMode mode, float deltaTime, float time) {
            if (_isDead)
                return;

            bool isSitting = mode == FieldAgentMode.Deck;
            _flinchSecondsLeft = Mathf.Max(0f, _flinchSecondsLeft - deltaTime);
            _frame.localRotation = Quaternion.Euler(_motion.CalculateGnawEuler(isSitting, time, _phase));
            _frame.localScale = _motion.CalculateFlinchScale(_flinchSecondsLeft);
            ShowWings(isSitting, time);
            ShowLegs(isSitting, time);
            Vector2 twitch = _motion.CalculateTailTwitch(isSitting, time, _phase);
            _tailBase.localRotation = _tailBaseRest * Quaternion.Euler(twitch.y, twitch.x, 0f);
        }

        public void PresentStrike() =>
            _flinchSecondsLeft = _feel.FlinchSeconds;

        public void PresentDeath() {
            _isDead = true;
            _frame.localScale = Vector3.zero;
            _targetCollider.SetStrikable(false);
        }

        private void ShowWings(bool isSitting, float time) {
            float degrees = _motion.CalculateWingDegrees(isSitting, time, _phase);
            _leftWing.localRotation = _leftWingRest * Quaternion.Euler(0f, 0f, degrees);
            _rightWing.localRotation = _rightWingRest * Quaternion.Euler(0f, 0f, degrees * MIRROR);
        }

        private void ShowLegs(bool isSitting, float time) {
            for (int i = 0; i < _legs.Length; i++)
                _legs[i].localRotation = _legRests[i] * Quaternion.Euler(_motion.CalculateLegDegrees(isSitting, time, _phase, i), 0f, 0f);
        }

        private void ResetLook() {
            _isDead = false;
            _flinchSecondsLeft = 0f;
            _frame.localRotation = Quaternion.identity;
            _frame.localScale = Vector3.one;
            _tailBase.localRotation = _tailBaseRest;
        }
    }
}
