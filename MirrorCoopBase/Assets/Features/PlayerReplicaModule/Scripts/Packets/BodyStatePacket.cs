using UnityEngine;

namespace Features.PlayerReplicaModule.Packets {
    // One snapshot of a player's body that its owner sends about 30 times a second over the unreliable channel. It is
    // always complete, never a difference to the previous one, so a lost packet breaks nothing. Positions, velocity and
    // the walk direction are in the axes of the platform the owner stands on (0 = the still world).
    // Public fields, so the network layer can write and read it without hand-written serializers.
    public struct BodyStatePacket {
        // Grows by one with every packet; packets older than the last applied one are dropped.
        public ushort Number;
        public uint CarrierId;
        public Vector3 RelativePosition;
        public Vector3 RelativeVelocity;
        // The walk input, already turned by the owner's camera.
        public Vector2 WalkDirection;
        // The look direction around the platform's up axis, see YawCompression.
        public ushort LookYaw;
        public BodyStateFlags States;
        // Grows by one with every jump, so a copy sees a jump even when the packet of that moment was lost.
        public byte JumpCount;
        // Set by the server on arrival: its estimate of when the owner took this snapshot, in server time.
        public double ServerTime;

        public bool Has(BodyStateFlags flag) =>
            (States & flag) != 0;
    }
}
