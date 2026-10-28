using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Steering;
using NUnit.Framework;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Editor.Tests {
    public sealed class FieldAgentSeparationTests {
        private const float STEP_SECONDS = 0.02f;
        private const float FLIGHT_SECONDS = 6f;
        private const int FLOCK_SIZE = 8;

        private FieldAgentProfile _profile;
        private FieldAgentServerModel _model;
        private FieldAgentSeparation _separation;
        private FieldAgentSteering _steering;

        [SetUp]
        public void SetUp() {
            _profile = FieldAgentTestAssets.CreateProfile();
            _model = new FieldAgentServerModel();
            _separation = new FieldAgentSeparation();
            _steering = new FieldAgentSteering();
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_profile);

        [Test]
        public void WhenAnotherAgentIsClose_ThenSeparationPushesAwayFromIt() {
            AddAgent(Vector3.zero);
            AddAgent(new Vector3(1f, 0f, 0f));

            Vector3 push = _separation.CalculateSeparation(0, _model, _profile);

            Assert.Less(push.x, 0f, $"The neighbour is on the right, the push must go left: {push}.");
        }

        [Test]
        public void WhenOtherAgentsAreFarOrOnTheDeck_ThenNoSeparation() {
            AddAgent(Vector3.zero);
            AddAgent(new Vector3(_profile.SeparationRadius + 1f, 0f, 0f));
            int deckAgent = AddAgent(new Vector3(0.5f, 0f, 0f));
            _model.TryFindIndex(deckAgent, out int deckIndex);
            _model.GetState(deckIndex).Mode = FieldAgentMode.Deck;

            Assert.AreEqual(Vector3.zero, _separation.CalculateSeparation(0, _model, _profile));
        }

        [Test]
        public void WhenAFlockSeeksOnePoint_ThenItsAgentsKeepTheirDistance() {
            for (int i = 0; i < FLOCK_SIZE; i++)
                AddAgent(new Vector3(i * 0.3f, 0f, -20f));

            Vector3 target = Vector3.zero;
            for (float time = 0f; time < FLIGHT_SECONDS; time += STEP_SECONDS)
                StepFlock(target);

            float closest = FindClosestPair();
            Assert.GreaterOrEqual(closest, _profile.SeparationRadius * 0.5f, $"Two agents of the flock are only {closest:0.00} m apart.");
        }

        private void StepFlock(Vector3 target) {
            for (int i = 0; i < _model.Count; i++) {
                ref FieldAgentState state = ref _model.GetState(i);
                Vector3 desired = _steering.CalculateSeek(state.Position, target, 1f, _profile) + _separation.CalculateSeparation(i, _model, _profile) * 1.2f;
                state.Velocity = _steering.LimitVelocity(state.Velocity, Vector3.ClampMagnitude(desired, _profile.MaxSpeed), _profile, STEP_SECONDS);
                state.Position += state.Velocity * STEP_SECONDS;
            }
        }

        private float FindClosestPair() {
            float closest = float.MaxValue;
            for (int i = 0; i < _model.Count; i++) {
                for (int j = i + 1; j < _model.Count; j++)
                    closest = Mathf.Min(closest, Vector3.Distance(_model.GetState(i).Position, _model.GetState(j).Position));
            }

            return closest;
        }

        private int AddAgent(Vector3 position) =>
            _model.Add(new FieldAgentState { Mode = FieldAgentMode.Flight, Position = position, Rotation = Quaternion.identity }, new IdleBehaviour(position, 0f, 0f, 0f));
    }
}
