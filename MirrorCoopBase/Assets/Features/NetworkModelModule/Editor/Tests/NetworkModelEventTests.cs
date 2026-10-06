using Features.NetworkModelModule.Scripts.Generated;
using NUnit.Framework;

namespace Features.NetworkModelModule.Scripts.Editor.Tests {
    public sealed class NetworkModelEventTests {
        [Test]
        public void ApplyCount_RaisesFieldAndModelEventsOnce() {
            DebugCounterModel model = new DebugCounterModel();
            int fieldEvents = 0;
            int anyEvents = 0;
            model.OnCountChanged += () => fieldEvents++;
            model.OnChanged += () => anyEvents++;

            model.ApplyCount(3);

            Assert.That(model.Count, Is.EqualTo(3));
            Assert.That(fieldEvents, Is.EqualTo(1));
            Assert.That(anyEvents, Is.EqualTo(1));

            model.ApplyCount(3);

            Assert.That(fieldEvents, Is.EqualTo(1));
            Assert.That(anyEvents, Is.EqualTo(1));
        }

        [Test]
        public void AddScores_RaisesAddedEvent() {
            DebugCounterModel model = new DebugCounterModel();
            int addedIndex = -1;
            int addedValue = 0;
            model.OnScoresAdded += (index, value) => {
                addedIndex = index;
                addedValue = value;
            };

            model.AddScores(0, 7);

            Assert.That(model.Scores.Count, Is.EqualTo(1));
            Assert.That(model.Scores[0], Is.EqualTo(7));
            Assert.That(addedIndex, Is.EqualTo(0));
            Assert.That(addedValue, Is.EqualTo(7));
        }

        [Test]
        public void ApplyState_FieldEventSeesTheWholeNewState() {
            AtomicPairModel model = new AtomicPairModel();
            int rightSeenOnLeftChanged = 0;
            model.OnLeftChanged += () => rightSeenOnLeftChanged = model.Right;

            model.ApplyState(new AtomicPairState { Left = 1, Right = 2 });

            Assert.That(rightSeenOnLeftChanged, Is.EqualTo(2));
        }
    }
}
