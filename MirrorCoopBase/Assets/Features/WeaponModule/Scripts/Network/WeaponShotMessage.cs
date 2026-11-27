using Features.WeaponModule.Scripts.Parts.Delivery;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Network {
    // One shot as it goes over the network: from the holder to the server, and from the server to every other peer.
    // Public fields: Mirror serializes them without allocating.
    public struct WeaponShotMessage {
        private const byte IMPACT = 1;
        private const byte TARGET_HIT = 2;
        private const byte RECEIVER_HIT = 4;

        public int WeaponIndex;
        public ushort Sequence;
        public Vector3 Origin;
        public Vector3 EndPoint;
        public Vector3 Normal;
        public byte Flags;
        public ulong TargetId;
        public uint ReceiverId;
        public short ReceiverPartIndex;

        public bool IsImpact => (Flags & IMPACT) != 0;
        public bool IsTargetHit => (Flags & TARGET_HIT) != 0;
        public bool HasReceiver => (Flags & RECEIVER_HIT) != 0;

        public Vector3 Direction {
            get {
                Vector3 toEnd = EndPoint - Origin;
                return toEnd.sqrMagnitude > 0f ? toEnd.normalized : Vector3.forward;
            }
        }

        public static WeaponShotMessage From(in WeaponShotOutcome outcome, int weaponIndex, ushort sequence) =>
            new WeaponShotMessage {
                WeaponIndex = weaponIndex,
                Sequence = sequence,
                Origin = outcome.Shot.Origin,
                EndPoint = outcome.EndPoint,
                Normal = outcome.Normal,
                Flags = (byte)((outcome.IsImpact ? IMPACT : 0) | (outcome.IsTargetHit ? TARGET_HIT : 0) | (outcome.HasReceiver ? RECEIVER_HIT : 0)),
                TargetId = outcome.TargetId,
                ReceiverId = outcome.ReceiverId,
                ReceiverPartIndex = (short)outcome.ReceiverPartIndex
            };
    }
}
