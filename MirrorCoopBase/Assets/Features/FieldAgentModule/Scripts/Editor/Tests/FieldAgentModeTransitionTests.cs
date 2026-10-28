using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Deck;
using NUnit.Framework;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Editor.Tests {
    public sealed class FieldAgentModeTransitionTests {
        private const float STEP_SECONDS = 0.02f;
        private const float TOLERANCE = 0.001f;
        private static readonly Vector3 _deckPoint = new(0f, 0.37f, -5f);

        private FieldAgentProfile _profile;
        private FieldAgentTransitionService _transition;
        private FieldAgentShipFrame _ship;

        [SetUp]
        public void SetUp() {
            _profile = FieldAgentTestAssets.CreateProfile();
            _transition = new FieldAgentTransitionService();
            _ship = new FieldAgentShipFrame(true, new Vector3(100f, 20f, 300f), Quaternion.Euler(0f, 70f, 5f), new Vector3(8f, 0f, 3f));
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_profile);

        [Test]
        public void WhenLanding_ThenTheAgentEasesInShipSpaceOntoTheDeckPointAndStandsOnTheDeck() {
            FieldAgentState state = CreateFlying(_ship.ToWorldPoint(_deckPoint + Vector3.up * 3f));

            _transition.BeginLanding(ref state, _ship, _deckPoint);
            Assert.AreEqual(FieldAgentMode.Landing, state.Mode);
            Assert.AreEqual(3f, state.Position.y - _deckPoint.y, TOLERANCE, "The landing starts where the agent was, in ship space.");

            for (float time = 0f; state.Mode == FieldAgentMode.Landing && time <= _profile.LandingSeconds + STEP_SECONDS; time += STEP_SECONDS)
                _transition.StepLanding(ref state, _profile, STEP_SECONDS);

            Assert.AreEqual(FieldAgentMode.Deck, state.Mode);
            Assert.AreEqual(_deckPoint, state.Position);
        }

        [Test]
        public void WhenTakingOff_ThenTheAgentRisesInShipSpaceAndFliesOnInTheWorldWithTheShipsVelocity() {
            FieldAgentState state = new() { Mode = FieldAgentMode.Deck, Position = _deckPoint, Rotation = Quaternion.identity };

            _transition.BeginTakeOff(ref state, _profile);
            for (float time = 0f; state.Mode == FieldAgentMode.TakingOff && time <= _profile.TakeOffSeconds + STEP_SECONDS; time += STEP_SECONDS)
                _transition.StepTakeOff(ref state, _profile, _ship, STEP_SECONDS);

            Assert.AreEqual(FieldAgentMode.Flight, state.Mode);
            Vector3 expected = _ship.ToWorldPoint(_deckPoint + Vector3.up * _profile.TakeOffHeight);
            Assert.Less(Vector3.Distance(expected, state.Position), TOLERANCE * 10f, $"The agent left the take-off at {state.Position}, expected {expected}.");
            Assert.Greater(Vector3.Dot(state.Velocity - _ship.Velocity, _ship.ToWorldDirection(Vector3.up)), 0f, "The agent must leave upward from the deck.");
        }

        [Test]
        public void WhenAttached_ThenTheBehaviourAsksForALandingAtItsShipPoint() {
            AttachBehaviour attach = new(_deckPoint, 0.3f);
            FieldAgentBehaviourInput flying = new(1, FieldAgentMode.Flight, Vector3.zero, Vector3.zero, _ship, 0f);
            FieldAgentBehaviourInput sitting = new(1, FieldAgentMode.Deck, _ship.ToWorldPoint(_deckPoint), _deckPoint, _ship, 0f);

            FieldAgentGoal goal = attach.Evaluate(flying);

            Assert.IsTrue(goal.IsShipSpace && goal.IsLandingOnArrival);
            Assert.IsFalse(attach.IsFinished(flying));
            Assert.IsTrue(attach.IsFinished(sitting));
        }

        [Test]
        public void WhenAgentsAreRemoved_ThenTheModelKeepsIdOrderAndFindsEveryOther() {
            FieldAgentServerModel model = new();
            for (int i = 0; i < 70; i++)
                model.Add(new FieldAgentState(), new IdleBehaviour(Vector3.zero, 0f, 0f));

            model.TryFindIndex(10, out int index);
            model.RemoveAt(index);

            Assert.AreEqual(69, model.Count);
            Assert.IsFalse(model.TryFindIndex(10, out _));
            Assert.IsTrue(model.TryFindIndex(70, out int last));
            Assert.AreEqual(68, last);
            for (int i = 1; i < model.Count; i++)
                Assert.Less(model.GetState(i - 1).Id, model.GetState(i).Id);
        }

        private static FieldAgentState CreateFlying(Vector3 position) =>
            new() { Mode = FieldAgentMode.Flight, Position = position, Rotation = Quaternion.identity };
    }
}
