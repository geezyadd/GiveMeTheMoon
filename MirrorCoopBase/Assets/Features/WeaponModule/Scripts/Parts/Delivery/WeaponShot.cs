using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // One shot born by a fire mode, before the delivery decides where it lands.
    public readonly struct WeaponShot {
        public string WeaponId { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public float RangeMeters { get; }
        // The network id of the shooter's own body, which the shot passes through; 0: none.
        public uint ShooterBodyId { get; }

        public WeaponShot(string weaponId, Vector3 origin, Vector3 direction, float rangeMeters, uint shooterBodyId = 0) {
            WeaponId = weaponId;
            Origin = origin;
            Direction = direction;
            RangeMeters = rangeMeters;
            ShooterBodyId = shooterBodyId;
        }
    }
}
