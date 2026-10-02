using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class RideOffsetBufferTests {
        private const double SEND_INTERVAL = 0.05d;
        private const float TOLERANCE = 0.0001f;

        private RideOffsetBuffer _buffer;

        [SetUp]
        public void SetUp() =>
            _buffer = new RideOffsetBuffer(SEND_INTERVAL);

        [Test]
        public void WhenSampledBetweenStamps_ThenOffsetIsInterpolatedByTime() {
            _buffer.Reset(10d, Vector3.zero);
            _buffer.Add(10.05d, new Vector3(1f, 0f, 0f));

            Vector3 offset = _buffer.Sample(10.025d);

            Assert.AreEqual(0.5f, offset.x, TOLERANCE);
        }

        [Test]
        public void WhenSampledAfterTheNewestStamp_ThenNewestOffsetIsHeld() {
            _buffer.Reset(10d, Vector3.zero);
            _buffer.Add(10.05d, new Vector3(1f, 0f, 2f));

            Vector3 offset = _buffer.Sample(11d);

            Assert.AreEqual(new Vector3(1f, 0f, 2f), offset);
        }

        [Test]
        public void WhenOwnerMovesAfterAPause_ThenRiderStaysPutUntilOneSendBeforeTheMove() {
            _buffer.Reset(10d, Vector3.zero);
            _buffer.Add(12d, new Vector3(0f, 0f, 1f));

            Vector3 duringPause = _buffer.Sample(11.9d);
            Vector3 midSend = _buffer.Sample(11.975d);

            Assert.AreEqual(Vector3.zero, duringPause);
            Assert.AreEqual(0.5f, midSend.z, TOLERANCE);
        }

        [Test]
        public void WhenStampRepeats_ThenNewestOffsetIsReplaced() {
            _buffer.Reset(10d, Vector3.zero);
            _buffer.Add(10d, Vector3.one);

            Assert.AreEqual(Vector3.one, _buffer.Sample(10d));
        }

        [Test]
        public void WhenMoreSamplesThanCapacity_ThenNewestStillSampleCorrectly() {
            _buffer.Reset(0d, Vector3.zero);
            for (int i = 1; i <= 40; i++)
                _buffer.Add(i * SEND_INTERVAL, new Vector3(i, 0f, 0f));

            Vector3 offset = _buffer.Sample(39.5d * SEND_INTERVAL);

            Assert.AreEqual(39.5f, offset.x, TOLERANCE);
        }
    }
}
