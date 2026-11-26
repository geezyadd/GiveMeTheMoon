using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // What a weapon ray struck: a surface, the id of the weapon target on it when there is one, and what shows the
    // strike on that surface when it shows one (null otherwise).
    public readonly struct WeaponRaycastHit {
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public bool HasTarget { get; }
        public ulong TargetId { get; }
        public IWeaponImpactFeedback ImpactFeedback { get; }
        // A body that reacts to the shot (a player's ragdoll), by its network id and the struck part's index.
        public bool HasReceiver { get; }
        public uint ReceiverId { get; }
        public int ReceiverPartIndex { get; }

        public WeaponRaycastHit(Vector3 point, Vector3 normal, bool hasTarget, ulong targetId, IWeaponImpactFeedback impactFeedback = null,
                                bool hasReceiver = false, uint receiverId = 0, int receiverPartIndex = -1) {
            Point = point;
            Normal = normal;
            HasTarget = hasTarget;
            TargetId = targetId;
            ImpactFeedback = impactFeedback;
            HasReceiver = hasReceiver;
            ReceiverId = receiverId;
            ReceiverPartIndex = receiverPartIndex;
        }
    }
}
