using System.Collections.Generic;
using Features.BugModule.Scripts.Behaviours;
using Features.BugModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Behaviours;
using NUnit.Framework;
using UnityEngine;

namespace Features.BugModule.Scripts.Editor.Tests {
    // The darting flight on top of a plain seek: it hangs nearly still for a part of every cycle, dashes off its line
    // to changing sides the rest of it, and flies straight in near its goal.
    public sealed class BugDartBehaviourTests {
        private const int BUG_ID = 3;
        private const float SAMPLE_SECONDS = 0.05f;
        private const int CYCLES_SAMPLED = 8;
        private const float FULL_SPEED = 1f;
        private const float ARRIVE_METERS = 1f;
        private const float MIN_OFF_LINE_METERS = 1f;
        private const float SHARE_TOLERANCE = 0.05f;

        private static readonly Vector3 _goal = new(0f, 0f, 200f);

        private BugConfiguration _configuration;
        private BugDartBehaviour _dart;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<BugConfiguration>();
            _dart = new BugDartBehaviour(new SeekBehaviour(_goal, false, ARRIVE_METERS, FULL_SPEED), _configuration);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_configuration);

        [Test]
        public void WhenTheBugFliesFarFromItsGoal_ThenItFreezesForItsShareOfEveryCycleAndDashesTheRest() {
            int frozen = 0;
            List<FieldAgentGoal> goals = SampleGoals(Vector3.zero);
            foreach (FieldAgentGoal goal in goals) {
                if (goal.SpeedShare < FULL_SPEED)
                    frozen++;
            }

            Assert.AreEqual(_configuration.FreezeShare, frozen / (float)goals.Count, SHARE_TOLERANCE);
        }

        [Test]
        public void WhenTheBugDashes_ThenItAimsOffItsStraightLineToBothSides() {
            bool hasLeft = false;
            bool hasRight = false;
            foreach (FieldAgentGoal goal in SampleGoals(Vector3.zero)) {
                if (Mathf.Approximately(goal.SpeedShare, FULL_SPEED) == false)
                    continue;

                hasLeft |= goal.Point.x < -MIN_OFF_LINE_METERS;
                hasRight |= goal.Point.x > MIN_OFF_LINE_METERS;
            }

            Assert.IsTrue(hasLeft && hasRight, "The dashes do not jerk to both sides of the line.");
        }

        [Test]
        public void WhenTheBugIsNearItsGoal_ThenItFliesStraightInAtFullSpeed() {
            Vector3 near = _goal - Vector3.forward * (_configuration.CalmNearGoalMeters / 2f);
            foreach (FieldAgentGoal goal in SampleGoals(near)) {
                Assert.AreEqual(_goal, goal.Point);
                Assert.AreEqual(FULL_SPEED, goal.SpeedShare);
            }
        }

        [Test]
        public void WhenTheInnerGoalIsReached_ThenTheDartIsFinishedToo() {
            Assert.IsFalse(_dart.IsFinished(CreateInput(Vector3.zero, 0f)));
            Assert.IsTrue(_dart.IsFinished(CreateInput(_goal, 0f)));
        }

        private List<FieldAgentGoal> SampleGoals(Vector3 position) {
            List<FieldAgentGoal> goals = new();
            int samples = Mathf.RoundToInt(_configuration.DartCycleSeconds * CYCLES_SAMPLED / SAMPLE_SECONDS);
            for (int i = 0; i < samples; i++)
                goals.Add(_dart.Evaluate(CreateInput(position, i * SAMPLE_SECONDS)));

            return goals;
        }

        private static FieldAgentBehaviourInput CreateInput(Vector3 position, float time) =>
            new(BUG_ID, FieldAgentMode.Flight, position, position, new FieldAgentShipFrame(false, Vector3.zero, Quaternion.identity, Vector3.zero), time);
    }
}
