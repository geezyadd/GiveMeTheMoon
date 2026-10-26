using UnityEngine;

namespace Features.GameCoreModule.Contracts {
    // One hit on the ship's hull before armor. The source id is a plain value the caller understands (a rock id,
    // an enemy id; 0 = none), so the hull does not depend on what hit it.
    public readonly struct ShipDamage {
        public float Amount { get; }
        public ShipDamageCause Cause { get; }
        public long SourceId { get; }
        public Vector3 HitPoint { get; }

        public ShipDamage(float amount, ShipDamageCause cause, Vector3 hitPoint, long sourceId = 0) {
            Amount = amount;
            Cause = cause;
            HitPoint = hitPoint;
            SourceId = sourceId;
        }
    }
}
