using System.Collections.Generic;
using Features.PlayerAnimationModule.Reach;
using NUnit.Framework;
using UnityEngine;

namespace Features.PlayerAnimationModule.Editor.Tests {
    public sealed class LimbReachRuleTests {
        private const float STEP_SECONDS = 0.02f;

        [Test]
        public void WhenNoRequestHasWeight_ThenLimbHasNoWinner() {
            LimbReachRequest request = CreateRequest(LimbId.RightHand, 0, 0, 0f);

            Assert.IsNull(LimbReachRule.PickWinner(new List<LimbReachRequest> { request }, LimbId.RightHand));
        }

        [Test]
        public void WhenRequestIsForAnotherLimb_ThenLimbHasNoWinner() {
            LimbReachRequest request = CreateRequest(LimbId.LeftHand, 0, 0, 1f);

            Assert.IsNull(LimbReachRule.PickWinner(new List<LimbReachRequest> { request }, LimbId.RightHand));
        }

        [Test]
        public void WhenRequestHasNoGoal_ThenLimbHasNoWinner() {
            LimbReachRequest request = new LimbReachRequest(LimbId.RightHand, 0, 0) { Weight = 1f };

            Assert.IsNull(LimbReachRule.PickWinner(new List<LimbReachRequest> { request }, LimbId.RightHand));
        }

        [Test]
        public void WhenTwoRequestsHaveWeight_ThenHigherPriorityWins() {
            LimbReachRequest low = CreateRequest(LimbId.RightHand, 0, 1, 1f);
            LimbReachRequest high = CreateRequest(LimbId.RightHand, 5, 0, 1f);

            Assert.AreSame(high, LimbReachRule.PickWinner(new List<LimbReachRequest> { low, high }, LimbId.RightHand));
        }

        [Test]
        public void WhenHigherPriorityRequestWaitsAtZeroWeight_ThenLowerOneWins() {
            LimbReachRequest low = CreateRequest(LimbId.RightHand, 0, 0, 1f);
            LimbReachRequest waiting = CreateRequest(LimbId.RightHand, 5, 1, 0f);

            Assert.AreSame(low, LimbReachRule.PickWinner(new List<LimbReachRequest> { low, waiting }, LimbId.RightHand));
        }

        [Test]
        public void WhenPrioritiesAreEqual_ThenLatestRequestWins() {
            LimbReachRequest first = CreateRequest(LimbId.RightHand, 0, 0, 1f);
            LimbReachRequest latest = CreateRequest(LimbId.RightHand, 0, 1, 1f);

            Assert.AreSame(latest, LimbReachRule.PickWinner(new List<LimbReachRequest> { latest, first }, LimbId.RightHand));
        }

        [Test]
        public void WhenRequestIsReleased_ThenItNoLongerWins() {
            LimbReachRequest request = CreateRequest(LimbId.RightHand, 0, 0, 1f);
            request.Release();

            Assert.IsNull(LimbReachRule.PickWinner(new List<LimbReachRequest> { request }, LimbId.RightHand));
        }

        [Test]
        public void WhenWeightBlends_ThenItMovesByStepOverBlendTime() {
            float weight = LimbReachRule.StepWeight(0f, 1f, 0.1f, STEP_SECONDS);

            Assert.AreEqual(0.2f, weight, 0.0001f);
        }

        [Test]
        public void WhenBlendTimeIsZero_ThenWeightJumpsAtOnce() {
            Assert.AreEqual(1f, LimbReachRule.StepWeight(0f, 1f, 0f, STEP_SECONDS));
        }

        [Test]
        public void WhenGoalIsSmoothed_ThenItMovesPartWayWithoutOvershoot() {
            Vector3 goal = LimbReachRule.StepGoal(Vector3.zero, Vector3.forward, 0.03f, STEP_SECONDS);

            Assert.Greater(goal.z, 0f);
            Assert.Less(goal.z, 1f);
        }

        [Test]
        public void WhenSmoothingIsZero_ThenGoalJumpsAtOnce() {
            Assert.AreEqual(Vector3.forward, LimbReachRule.StepGoal(Vector3.zero, Vector3.forward, 0f, STEP_SECONDS));
        }

        private static LimbReachRequest CreateRequest(LimbId limb, int priority, int order, float weight) {
            LimbReachRequest request = new LimbReachRequest(limb, priority, order) { Weight = weight };
            request.SetGoal(Vector3.one);
            return request;
        }
    }
}
