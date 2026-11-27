using System.Collections.Generic;
using NUnit.Framework;

namespace Features.BehaviourTreeModule.Editor.Tests {
    public sealed class BtPriorityTests {
        private const float FRAME = 0.1f;
        private const float LONG_RECHARGE = 5f;
        private const string HIGH = "High";
        private const string LOW = "Low";

        private readonly List<string> _journal = new();

        private bool _isHighReady;

        [SetUp]
        public void SetUp() {
            _journal.Clear();
            _isHighReady = false;
        }

        [Test]
        public void WhenAHigherBranchBecomesReady_ThenTheLowerBranchStopsBeforeTheHigherStarts() {
            BtTestTree tree = BtTestTree.CreateBuilt(builder => builder
                .Priority("Root")
                    .Guard("High ready", IsHighReady).Add(new BtRecordingStep(HIGH, _journal, BtStatus.Running))
                    .Add(new BtRecordingStep(LOW, _journal, BtStatus.Running))
                .End()
                .Build());
            tree.Update(FRAME);
            _journal.Clear();
            _isHighReady = true;

            tree.Update(FRAME);

            CollectionAssert.AreEqual(new[] { BtRecordingStep.STOP + LOW, BtRecordingStep.START + HIGH, BtRecordingStep.RUN + HIGH }, _journal);
        }

        [Test]
        public void WhenTheHigherBranchIsRecharging_ThenTheRunningLowerBranchIsNotInterrupted() {
            BtTestTree tree = BtTestTree.CreateBuilt(builder => builder
                .Priority("Root")
                    .Recharge("High recharge", LONG_RECHARGE).Add(new BtRecordingStep(HIGH, _journal, BtStatus.Success))
                    .Add(new BtRecordingStep(LOW, _journal, BtStatus.Running))
                .End()
                .Build());
            tree.Update(FRAME);
            tree.Update(FRAME);
            _journal.Clear();

            tree.Update(FRAME);

            CollectionAssert.AreEqual(new[] { BtRecordingStep.RUN + LOW }, _journal);
        }

        private bool IsHighReady() =>
            _isHighReady;
    }
}
