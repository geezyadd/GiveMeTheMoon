using Features.BodySyncModule.Packets;
using UnityEngine;

namespace Features.BodySyncModule.Sending {
    // Decides when the owner sends its next state packet: often while anything moves or changes, rarely while the body
    // stands still, as a sign of life. A change right after a still spell goes out at once, because the long idle
    // wait has already passed.
    public sealed class BodySendSchedule {
        private bool _hasSent;
        private double _lastSentAt;
        private BodyStatePacket _lastSent;

        // fresh: the packet as it would go now (its sequence does not matter).
        public bool IsDue(in BodyStatePacket fresh, double now, float sendInterval, float idleSendInterval, float idleSpeed) {
            if (_hasSent == false)
                return true;

            float interval = IsActive(fresh, idleSpeed) ? sendInterval : idleSendInterval;
            return now - _lastSentAt >= interval;
        }

        public void MarkSent(in BodyStatePacket packet, double now) {
            _hasSent = true;
            _lastSentAt = now;
            _lastSent = packet;
        }

        private bool IsActive(in BodyStatePacket fresh, float idleSpeed) =>
            fresh.LocalVelocity.magnitude > idleSpeed
            || fresh.Wish != Vector2.zero
            || fresh.Flags != _lastSent.Flags
            || fresh.JumpCounter != _lastSent.JumpCounter
            || fresh.PlatformId != _lastSent.PlatformId
            || fresh.FacingYaw != _lastSent.FacingYaw;
    }
}
