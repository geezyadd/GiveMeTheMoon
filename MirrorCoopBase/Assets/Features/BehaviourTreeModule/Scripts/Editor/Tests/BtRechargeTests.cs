using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Features.BehaviourTreeModule.Editor.Tests {
    public sealed class BtRechargeTests {
        private const float RECHARGE = 2f;
        private const float FRAME = 1f;
        private const string ATTACK = "Attack";
        private const string IDLE = "Idle";

        private readonly List<string> _journal = new();

        [SetUp]
        public void SetUp() =>
            _journal.Clear();

        [Test]
        public void WhenTheChildSucceeded_ThenTheRechargeFailsWithoutTickingTheChild() {
            BtTestTree tree = BtTestTree.CreateBuilt(builder => builder
                .Recharge("Recharge", RECHARGE).Add(new BtRecordingStep(ATTACK, _journal, BtStatus.Success))
                .Build());
            tree.Update(FRAME);

            BtStatus result = tree.Update(FRAME);

            Assert.AreEqual(BtStatus.Failure, result);
            Assert.AreEqual(1, CountEntries(BtRecordingStep.RUN + ATTACK));
        }

        [Test]
        public void WhenTheRechargingBranchIsSkipped_ThenItsTimeStillPassesAndItTakesOverWhenReady() {
            BtTestTree tree = BtTestTree.CreateBuilt(builder => builder
                .Priority("Root")
                    .Recharge("Recharge", RECHARGE).Add(new BtRecordingStep(ATTACK, _journal, BtStatus.Success))
                    .Add(new BtRecordingStep(IDLE, _journal, BtStatus.Running))
                .End()
                .Build());
            tree.Update(FRAME);
            tree.Update(FRAME);
            _journal.Clear();

            tree.Update(FRAME);

            CollectionAssert.AreEqual(new[] { BtRecordingStep.STOP + IDLE, BtRecordingStep.START + ATTACK, BtRecordingStep.RUN + ATTACK,
                BtRecordingStep.STOP + ATTACK }, _journal);
        }

        private int CountEntries(string entry) =>
            _journal.Count(journalEntry => journalEntry == entry);
    }
}
