using UnityEngine;

namespace Features.WeaponModule.Contracts {
    // One shot striking a body that reacts to it (a player's ragdoll), as every peer applies it to its own copy: where
    // it struck, along which direction, the velocity change it gives the struck part and how long the body goes limp
    // (0: it is only pushed).
    public readonly struct WeaponImpact {
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
        public float PushSpeed { get; }
        public float KnockdownSeconds { get; }
        public int PartIndex { get; }

        public WeaponImpact(Vector3 point, Vector3 direction, float pushSpeed, float knockdownSeconds, int partIndex) {
            Point = point;
            Direction = direction;
            PushSpeed = pushSpeed;
            KnockdownSeconds = knockdownSeconds;
            PartIndex = partIndex;
        }
    }
}
