using System.Collections.Generic;
using NUnit.Framework;

namespace Features.BehaviourTreeModule.Editor.Tests {
    public sealed class BtScoredChoiceTests {
        private const float FRAME = 0.1f;
        private const float STICKINESS = 0.2f;
        private const float FIRST_SCORE = 1f;
        private const float LOWER_SCORE = 0.5f;
        private const float SLIGHTLY_HIGHER_SCORE = 1.1f;
        private const float CLEARLY_HIGHER_SCORE = 1.3f;
        private const string CHASE = "Chase";
        private const string FLEE = "Flee";

        private readonly List<string> _journal = new();

        private float _chaseScore;
        private float _fleeScore;
        private BtRecordingStep _chase;
        private BtTestTree _tree;

        [SetUp]
        public void SetUp() {
            _journal.Clear();
            _chaseScore = FIRST_SCORE;
            _fleeScore = LOWER_SCORE;
            _chase = new BtRecordingStep(CHASE, _journal, BtStatus.Running);
            _tree = BtTestTree.CreateBuilt(builder => builder
                .ScoredChoice("Root", STICKINESS)
                    .Scored("Chase score", GetChaseScore).Add(_chase)
                    .Scored("Flee score", GetFleeScore).Add(new BtRecordingStep(FLEE, _journal, BtStatus.Running))
                .End()
                .Build());
        }

        [Test]
        public void WhenAnotherScoreRisesWithinTheStickiness_ThenTheRunningOptionKeepsRunning() {
            _tree.Update(FRAME);
            _journal.Clear();
            _fleeScore = SLIGHTLY_HIGHER_SCORE;

            _tree.Update(FRAME);

            CollectionAssert.AreEqual(new[] { BtRecordingStep.RUN + CHASE }, _journal);
        }

        [Test]
        public void WhenAnotherScoreRisesPastTheStickiness_ThenTheRunningOptionStopsBeforeTheOtherStarts() {
            _tree.Update(FRAME);
            _journal.Clear();
            _fleeScore = CLEARLY_HIGHER_SCORE;

            _tree.Update(FRAME);

            CollectionAssert.AreEqual(new[] { BtRecordingStep.STOP + CHASE, BtRecordingStep.START + FLEE, BtRecordingStep.RUN + FLEE }, _journal);
        }

        [Test]
        public void WhenTheBestOptionFails_ThenTheNextBestRunsInTheSameTick() {
            _chase.Result = BtStatus.Failure;

            BtStatus result = _tree.Update(FRAME);

            Assert.AreEqual(BtStatus.Running, result);
            CollectionAssert.Contains(_journal, BtRecordingStep.RUN + FLEE);
        }

        [Test]
        public void WhenNoScoreIsPositive_ThenTheChoiceFails() {
            _chaseScore = 0f;
            _fleeScore = 0f;

            BtStatus result = _tree.Update(FRAME);

            Assert.AreEqual(BtStatus.Failure, result);
        }

        private float GetChaseScore() =>
            _chaseScore;

        private float GetFleeScore() =>
            _fleeScore;
    }
}
