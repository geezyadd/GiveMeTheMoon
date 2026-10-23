using UnityEngine;

namespace Features.GameCoreModule.Contracts {
    // One hit on the ship's hull before armor. The source id is a plain value the caller understands (a rock id,
    // an enemy id), so the hull does not depend on what hit it.
    public readonly struct ShipDamage {
        public const long NO_SOURCE = 0;

        public float Amount { get; }
        public ShipDamageCause Cause { get; }
        public long SourceId { get; }
        public Vector3 HitPoint { get; }
        public bool HasSource => SourceId != NO_SOURCE;

        public ShipDamage(float amount, ShipDamageCause cause, Vector3 hitPoint, long sourceId = NO_SOURCE) {
            Amount = amount;
            Cause = cause;
            HitPoint = hitPoint;
            SourceId = sourceId;
        }
    }
}
