using System.Collections.Generic;
using NUnit.Framework;

namespace Features.BehaviourTreeModule.Editor.Tests {
    public sealed class BtGuardTests {
        private const float FRAME = 0.1f;
        private const string STEP = "Step";

        private readonly List<string> _journal = new();

        private bool _doesGuardHold;
        private BtRecordingStep _step;
        private BtTestTree _tree;

        [SetUp]
        public void SetUp() {
            _journal.Clear();
            _doesGuardHold = true;
            _step = new BtRecordingStep(STEP, _journal, BtStatus.Running);
            _tree = BtTestTree.CreateBuilt(builder => builder
                .Guard("Guard", DoesGuardHold).Add(_step)
                .Build());
        }

        [Test]
        public void WhenThePredicateHolds_ThenTheChildRuns() {
            BtStatus result = _tree.Update(FRAME);

            Assert.AreEqual(BtStatus.Running, result);
            Assert.AreEqual(BtStatus.Running, _step.Status);
        }

        [Test]
        public void WhenThePredicateStopsHolding_ThenTheChildIsInterruptedAndTheGuardFails() {
            _tree.Update(FRAME);
            _doesGuardHold = false;

            BtStatus result = _tree.Update(FRAME);

            Assert.AreEqual(BtStatus.Failure, result);
            Assert.AreEqual(BtStatus.Inactive, _step.Status);
            CollectionAssert.Contains(_journal, BtRecordingStep.STOP + STEP);
        }

        [Test]
        public void WhenThePredicateDoesNotHold_ThenTheChildNeverStarts() {
            _doesGuardHold = false;

            _tree.Update(FRAME);

            CollectionAssert.IsEmpty(_journal);
        }

        private bool DoesGuardHold() =>
            _doesGuardHold;
    }
}
