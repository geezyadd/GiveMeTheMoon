using UnityEngine;

namespace Features.ExplosionModule.Contracts {
    // What an explosion's wave brings to a receiver when its front reaches it: where it went off, how fast it throws
    // things at the receiver's distance and how long a ragdoll it reaches lies limp (0: too far to knock down).
    public readonly struct ExplosionHit {
        public readonly Vector3 Center;
        public readonly float Speed;
        public readonly float UpwardShare;
        public readonly float KnockdownSeconds;

        public ExplosionHit(Vector3 center, float speed, float upwardShare, float knockdownSeconds) {
            Center = center;
            Speed = speed;
            UpwardShare = upwardShare;
            KnockdownSeconds = knockdownSeconds;
        }

        public bool KnocksDown => KnockdownSeconds > 0f;

        // The velocity change the wave gives a body at this point: away from the centre, tipped up by the upward share.
        public Vector3 VelocityAt(Vector3 point) {
            Vector3 away = point - Center;
            Vector3 outward = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.up;
            Vector3 direction = (outward + Vector3.up * UpwardShare).normalized;
            return direction * Speed;
        }
    }
}
