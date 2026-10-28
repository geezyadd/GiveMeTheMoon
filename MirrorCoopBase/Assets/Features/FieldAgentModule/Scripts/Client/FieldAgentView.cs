using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Client {
    // A pooled agent look: the root takes the interpolated pose, the body child gets the local feel (bank, tilt, bob).
    // No logic of its own beyond applying FieldAgentViewFeel; a kind's own parts (IFieldAgentViewLifetime,
    // IFieldAgentViewMotion) on its children are told when the look starts, ends and is shown.
    public sealed class FieldAgentView : MonoBehaviour {
        // Spreads the bob of neighbouring agents, so a flock does not bob in step.
        private const float BOB_PHASE_PER_ID = 0.37f;

        [SerializeField] private Transform _body;

        private readonly FieldAgentViewFeel _feel = new();
        private readonly List<IFieldAgentViewLifetime> _lifetimes = new();
        private readonly List<IFieldAgentViewMotion> _motions = new();
        private FieldAgentProfile _profile;
        private float _bobPhase;

        private void Awake() {
            GetComponentsInChildren(true, _lifetimes);
            GetComponentsInChildren(true, _motions);
        }

        public void Begin(FieldAgentProfile profile, int agentId) {
            _profile = profile;
            _bobPhase = agentId * BOB_PHASE_PER_ID;
            _feel.Reset();
            _body.localRotation = Quaternion.identity;
            _body.localPosition = Vector3.zero;
            gameObject.SetActive(true);
            foreach (IFieldAgentViewLifetime lifetime in _lifetimes)
                lifetime.Begin(agentId);
        }

        public void End() {
            foreach (IFieldAgentViewLifetime lifetime in _lifetimes)
                lifetime.End();

            _feel.Reset();
            gameObject.SetActive(false);
        }

        public void ApplyPose(Vector3 position, Quaternion rotation, FieldAgentMode mode, float deltaTime, float time) {
            transform.SetPositionAndRotation(position, rotation);
            _feel.Update(position, rotation, deltaTime, _profile);
            _body.localRotation = Quaternion.Euler(_feel.Tilt, 0f, _feel.Bank);
            _body.localPosition = Vector3.up * _feel.CalculateBob(time, _bobPhase, _profile);
            foreach (IFieldAgentViewMotion motion in _motions)
                motion.Show(mode, deltaTime, time);
        }
    }
}
