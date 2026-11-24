using Features.BodySyncModule.Packets;
using UnityEngine;

namespace Features.BodySyncModule.Sending {
    // Decides when the owner sends its next state packet: at once when a control changes (walk pressed or released,
    // sprint, jump, standing or flying, a jump), so copies learn of a start or a stop as early as possible; often while
    // anything moves or the look turns; rarely while the body stands still, as a sign of life.
    public sealed class BodySendSchedule {
        private bool _hasSent;
        private double _lastSentAt;
        private BodyStatePacket _lastSent;

        // fresh: the packet as it would go now (its sequence does not matter).
        public bool IsDue(in BodyStatePacket fresh, double now, float sendInterval, float idleSendInterval, float idleSpeed) {
            if (_hasSent == false || ControlsChanged(fresh))
                return true;

            float interval = IsActive(fresh, idleSpeed) ? sendInterval : idleSendInterval;
            return now - _lastSentAt >= interval;
        }

        public void MarkSent(in BodyStatePacket packet, double now) {
            _hasSent = true;
            _lastSentAt = now;
            _lastSent = packet;
        }

        // Discrete changes only: the walk direction turning with the camera is not one, or packets would go every step.
        private bool ControlsChanged(in BodyStatePacket fresh) =>
            (fresh.Wish == Vector2.zero) != (_lastSent.Wish == Vector2.zero)
            || fresh.Flags != _lastSent.Flags
            || fresh.JumpCounter != _lastSent.JumpCounter
            || fresh.PlatformId != _lastSent.PlatformId;

        private bool IsActive(in BodyStatePacket fresh, float idleSpeed) =>
            fresh.LocalVelocity.magnitude > idleSpeed
            || fresh.Wish != Vector2.zero
            || fresh.FacingYaw != _lastSent.FacingYaw;
    }
}
