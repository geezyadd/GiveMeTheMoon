using UnityEngine;

namespace Features.WeaponModule.Scripts.Hits {
    // What the server itself knows when a hit report arrives; the report's own claims are never trusted for these.
    public readonly struct WeaponHitCircumstances {
        public WeaponHitCircumstances(string heldWeaponId, Vector3 shooterPosition, bool isTargetAlive, double serverTimeSeconds) {
            HeldWeaponId = heldWeaponId;
            ShooterPosition = shooterPosition;
            IsTargetAlive = isTargetAlive;
            ServerTimeSeconds = serverTimeSeconds;
        }

        // The weapon the shooter holds on the server; empty when it holds none.
        public string HeldWeaponId { get; }
        public Vector3 ShooterPosition { get; }
        public bool IsTargetAlive { get; }
        public double ServerTimeSeconds { get; }
    }
}
