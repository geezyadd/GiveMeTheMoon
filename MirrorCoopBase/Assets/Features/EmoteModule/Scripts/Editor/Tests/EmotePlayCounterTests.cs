using NUnit.Framework;

namespace Features.EmoteModule.Editor.Tests {
    public sealed class EmotePlayCounterTests {
        [Test]
        public void WhenTheFirstCountIsRead_ThenItIsNoPlay() =>
            Assert.IsFalse(new EmotePlayCounter().Step(true, 7));

        [Test]
        public void WhenTheCountGrows_ThenItIsOnePlay() {
            var counter = new EmotePlayCounter();
            counter.Step(true, 3);

            Assert.IsTrue(counter.Step(true, 4));
            Assert.IsFalse(counter.Step(true, 4));
        }

        [Test]
        public void WhenSeveralPlaysWereMissed_ThenItIsOnePlay() {
            var counter = new EmotePlayCounter();
            counter.Step(true, 3);

            Assert.IsTrue(counter.Step(true, 6));
            Assert.IsFalse(counter.Step(true, 6));
        }

        [Test]
        public void WhenTheCountWraps_ThenItIsAPlay() {
            var counter = new EmotePlayCounter();
            counter.Step(true, 255);

            Assert.IsTrue(counter.Step(true, 0));
        }

        [Test]
        public void WhenThePlaysWereUnavailable_ThenTheNextCountIsNoPlay() {
            var counter = new EmotePlayCounter();
            counter.Step(true, 3);
            Assert.IsFalse(counter.Step(false, 5));

            Assert.IsFalse(counter.Step(true, 5));
            Assert.IsTrue(counter.Step(true, 6));
        }
    }
}
