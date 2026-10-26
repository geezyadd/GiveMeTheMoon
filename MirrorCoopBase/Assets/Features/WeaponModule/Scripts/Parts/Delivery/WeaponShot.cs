using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // One shot born by a fire mode, before the delivery decides where it lands.
    public readonly struct WeaponShot {
        public string WeaponId { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public float RangeMeters { get; }

        public WeaponShot(string weaponId, Vector3 origin, Vector3 direction, float rangeMeters) {
            WeaponId = weaponId;
            Origin = origin;
            Direction = direction;
            RangeMeters = rangeMeters;
        }
    }
}
