using NUnit.Framework;

namespace Features.AttackModule.Editor.Tests {
    public sealed class AttackPressCounterTests {
        private AttackPressCounter _counter;

        [SetUp]
        public void SetUp() =>
            _counter = new AttackPressCounter();

        [Test]
        public void WhenTheFirstCountIsRead_ThenThereIsNoPress() {
            Assert.IsFalse(_counter.Step(true, 4));
        }

        [Test]
        public void WhenTheCountGrows_ThenThereIsOnePress() {
            _counter.Step(true, 4);

            Assert.IsTrue(_counter.Step(true, 5));
            Assert.IsFalse(_counter.Step(true, 5));
        }

        [Test]
        public void WhenSeveralPressesWereMissed_ThenThereIsStillOnePress() {
            _counter.Step(true, 4);

            Assert.IsTrue(_counter.Step(true, 7));
            Assert.IsFalse(_counter.Step(true, 7));
        }

        [Test]
        public void WhenTheCountWraps_ThenItIsAPress() {
            _counter.Step(true, 255);

            Assert.IsTrue(_counter.Step(true, 0));
        }

        [Test]
        public void WhenThePressesWereUnavailable_ThenTheNextCountIsNoPress() {
            _counter.Step(true, 4);
            Assert.IsFalse(_counter.Step(false, 6));

            Assert.IsFalse(_counter.Step(true, 6));
            Assert.IsTrue(_counter.Step(true, 7));
        }

        [Test]
        public void WhenTheCounterIsReset_ThenTheNextCountIsNoPress() {
            _counter.Step(true, 4);
            _counter.Reset();

            Assert.IsFalse(_counter.Step(true, 9));
        }
    }
}
