using System;

namespace Features.PlayerReplicaModule.Packets {
    // The owner's on / off states in one byte of the state packet.
    [Flags]
    public enum BodyStateFlags : byte {
        None = 0,
        Sprinting = 1 << 0,
        Grounded = 1 << 1,
        JumpHeld = 1 << 2,
        Seated = 1 << 3,
        Dead = 1 << 4
    }
}
