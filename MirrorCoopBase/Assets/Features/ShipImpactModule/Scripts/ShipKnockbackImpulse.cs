using UnityEngine;

namespace Features.ShipImpactModule.Scripts {
    // A knockback for ShipKnockback: the velocity it adds, how long it fades and its acceleration limit.
    public readonly struct ShipKnockbackImpulse {
        public Vector3 Velocity { get; }
        public float FadeSeconds { get; }
        public float MaxAcceleration { get; }

        public ShipKnockbackImpulse(Vector3 velocity, float fadeSeconds, float maxAcceleration) {
            Velocity = velocity;
            FadeSeconds = fadeSeconds;
            MaxAcceleration = maxAcceleration;
        }
    }
}
