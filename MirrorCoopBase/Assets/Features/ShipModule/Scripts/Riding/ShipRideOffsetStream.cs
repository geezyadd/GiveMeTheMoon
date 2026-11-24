using UnityEngine;

namespace Features.ShipModule.Scripts.Riding {
    // A standing rider's place on the deck over the network: the owner sends its offset every 0.05 s while it changes,
    // and every other peer plays the received offsets back a little late (two send intervals) from a buffer.
    internal sealed class ShipRideOffsetStream {
        private const float SEND_SECONDS = 0.05f;
        private const float RESEND_SQR_METERS = 0.000001f;
        private const double REMOTE_DELAY = 2d * SEND_SECONDS;

        private readonly RideOffsetBuffer _remoteOffsets = new RideOffsetBuffer(SEND_SECONDS);

        private float _nextSend;

        // The owner: whether to send the offset now (the send interval is up and it moved since the synced one).
        public bool ShouldSend(Vector3 syncedOffset, Vector3 localOffset) {
            if (Time.unscaledTime < _nextSend)
                return false;

            _nextSend = Time.unscaledTime + SEND_SECONDS;
            return (syncedOffset - localOffset).sqrMagnitude > RESEND_SQR_METERS;
        }

        public void Reset(double time, Vector3 offset) =>
            _remoteOffsets.Reset(time, offset);

        public void Add(double time, Vector3 offset) =>
            _remoteOffsets.Add(time, offset);

        // Another peer: the offset to show now.
        public Vector3 Sample(double networkTime) =>
            _remoteOffsets.Sample(networkTime - REMOTE_DELAY);
    }
}
