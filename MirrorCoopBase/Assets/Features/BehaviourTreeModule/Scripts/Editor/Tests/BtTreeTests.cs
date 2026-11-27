using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Features.BehaviourTreeModule.Editor.Tests {
    public sealed class BtTreeTests {
        private const float FRAME = 0.25f;
        private const float TOLERANCE = 0.0001f;
        private const string STEP = "Step";

        private readonly List<string> _journal = new();

        [SetUp]
        public void SetUp() =>
            _journal.Clear();

        [Test]
        public void WhenUpdatedBeforeItIsBuilt_ThenTheTreeThrows() {
            BtTestTree tree = new(DescribeRunningStep);

            Assert.Throws<InvalidOperationException>(() => tree.Update(FRAME));
        }

        [Test]
        public void WhenUpdatedAfterItIsDisposed_ThenTheTreeThrows() {
            BtTestTree tree = BtTestTree.CreateBuilt(DescribeRunningStep);
            tree.Dispose();

            Assert.Throws<ObjectDisposedException>(() => tree.Update(FRAME));
        }

        [Test]
        public void WhenUpdatedTwice_ThenTheClockCountsTheTicksAndSumsTheirTime() {
            BtTestTree tree = BtTestTree.CreateBuilt(DescribeRunningStep);

            tree.Update(FRAME);
            tree.Update(FRAME);

            Assert.AreEqual(2, tree.Clock.TickCount);
            Assert.AreEqual(FRAME * 2f, tree.Clock.ElapsedSeconds, TOLERANCE);
        }

        [Test]
        public void WhenHalted_ThenTheRunningStepStops() {
            BtTestTree tree = BtTestTree.CreateBuilt(DescribeRunningStep);
            tree.Update(FRAME);

            tree.Halt();

            CollectionAssert.Contains(_journal, BtRecordingStep.STOP + STEP);
            Assert.AreEqual(BtStatus.Inactive, tree.Root.Status);
        }

        [Test]
        public void WhenADecoratorIsLeftWithoutAChild_ThenBuildingThrows() {
            BtTestTree tree = new(DescribeGuardWithoutChild);

            Assert.Throws<InvalidOperationException>(tree.Build);
        }

        private BtNode DescribeRunningStep(BtBuilder builder) =>
            builder.Add(new BtRecordingStep(STEP, _journal, BtStatus.Running)).Build();

        private static BtNode DescribeGuardWithoutChild(BtBuilder builder) =>
            builder.Sequence("Root").Guard("Guard", AlwaysHolds).End().Build();

        private static bool AlwaysHolds() =>
            true;
    }
}
