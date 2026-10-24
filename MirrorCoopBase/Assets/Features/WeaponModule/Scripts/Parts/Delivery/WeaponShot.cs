using Features.NetworkModelModule.Scripts;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // One shot born by a fire mode, before the delivery decides where it lands.
    public readonly struct WeaponShot {
        public string WeaponId { get; }
        public PlayerKey Shooter { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public float RangeMeters { get; }

        public WeaponShot(string weaponId, PlayerKey shooter, Vector3 origin, Vector3 direction, float rangeMeters) {
            WeaponId = weaponId;
            Shooter = shooter;
            Origin = origin;
            Direction = direction;
            RangeMeters = rangeMeters;
        }
    }
}
