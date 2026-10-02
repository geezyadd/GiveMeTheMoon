using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Ship-local offsets of a remote rider, stamped with the owner's network time and played back with a fixed delay, so
    // every peer shows the rider where its owner was at the same server time (arrival jitter does not move it).
    public sealed class RideOffsetBuffer {
        private const int CAPACITY = 16;
        private const double HOLD_GAP_SENDS = 2d;

        private readonly double[] _times = new double[CAPACITY];
        private readonly Vector3[] _offsets = new Vector3[CAPACITY];
        private readonly double _sendInterval;
        private int _count;

        public RideOffsetBuffer(double sendInterval) =>
            _sendInterval = sendInterval;

        public void Reset(double time, Vector3 offset) {
            _count = 0;
            Push(time, offset);
        }

        public void Add(double time, Vector3 offset) {
            if (_count == 0) {
                Push(time, offset);
                return;
            }

            double lastTime = _times[_count - 1];
            if (time <= lastTime) {
                _offsets[_count - 1] = offset;
                return;
            }

            // The owner only sends while it moves: after a pause it stood at the previous offset until about one send
            // interval before this sample, not drifting there over the whole pause.
            if (time - lastTime > HOLD_GAP_SENDS * _sendInterval)
                Push(time - _sendInterval, _offsets[_count - 1]);

            Push(time, offset);
        }

        public Vector3 Sample(double renderTime) {
            if (_count == 0)
                throw new System.InvalidOperationException("RideOffsetBuffer.Sample before the first Reset.");

            if (renderTime <= _times[0])
                return _offsets[0];

            for (int i = 1; i < _count; i++) {
                if (renderTime > _times[i])
                    continue;

                double span = _times[i] - _times[i - 1];
                float blend = (float)((renderTime - _times[i - 1]) / span);
                return Vector3.Lerp(_offsets[i - 1], _offsets[i], blend);
            }

            return _offsets[_count - 1];
        }

        private void Push(double time, Vector3 offset) {
            if (_count == CAPACITY) {
                System.Array.Copy(_times, 1, _times, 0, CAPACITY - 1);
                System.Array.Copy(_offsets, 1, _offsets, 0, CAPACITY - 1);
                _count -= 1;
            }

            _times[_count] = time;
            _offsets[_count] = offset;
            _count += 1;
        }
    }
}
