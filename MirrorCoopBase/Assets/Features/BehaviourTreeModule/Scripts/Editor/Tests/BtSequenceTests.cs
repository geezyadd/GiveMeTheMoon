using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Features.BehaviourTreeModule.Editor.Tests {
    public sealed class BtSequenceTests {
        private const float FRAME = 0.1f;
        private const int TICKS = 3;
        private const string FIRST = "First";
        private const string SECOND = "Second";

        private readonly List<string> _journal = new();

        private bool _doesCheckHold;

        [SetUp]
        public void SetUp() {
            _journal.Clear();
            _doesCheckHold = true;
        }

        [Test]
        public void WhenAStepSucceeded_ThenItIsNotRunAgainWhileTheNextStepRuns() {
            BtTestTree tree = BtTestTree.CreateBuilt(builder => builder
                .Sequence("Root")
                    .Add(new BtRecordingStep(FIRST, _journal, BtStatus.Success))
                    .Add(new BtRecordingStep(SECOND, _journal, BtStatus.Running))
                .End()
                .Build());

            for (int i = 0; i < TICKS; i++)
                tree.Update(FRAME);

            Assert.AreEqual(1, CountEntries(BtRecordingStep.RUN + FIRST));
            Assert.AreEqual(TICKS, CountEntries(BtRecordingStep.RUN + SECOND));
        }

        [Test]
        public void WhenAPassedCheckStopsHolding_ThenTheRunningStepIsInterruptedAndTheSequenceFails() {
            BtRecordingStep second = new(SECOND, _journal, BtStatus.Running);
            BtTestTree tree = BtTestTree.CreateBuilt(builder => builder
                .Sequence("Root")
                    .Check("Holds", DoesCheckHold)
                    .Add(second)
                .End()
                .Build());
            tree.Update(FRAME);
            _doesCheckHold = false;

            BtStatus result = tree.Update(FRAME);

            Assert.AreEqual(BtStatus.Failure, result);
            Assert.AreEqual(BtStatus.Inactive, second.Status);
            Assert.AreEqual(BtRecordingStep.STOP + SECOND, _journal.Last());
        }

        private int CountEntries(string entry) =>
            _journal.Count(journalEntry => journalEntry == entry);

        private bool DoesCheckHold() =>
            _doesCheckHold;
    }
}
