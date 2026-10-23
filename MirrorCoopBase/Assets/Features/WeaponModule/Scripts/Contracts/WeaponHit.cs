using Features.NetworkModelModule.Scripts;
using UnityEngine;

namespace Features.WeaponModule.Contracts {
    // One hit of one weapon on one target, as the server applies it. The target id is a plain value the target's own
    // module gives meaning to (a rock id, an enemy id), so targets do not need to know the weapon module.
    public readonly struct WeaponHit {
        public WeaponHit(ulong targetId, Vector3 point, Vector3 normal, float damage, PlayerKey shooter, string weaponId) {
            TargetId = targetId;
            Point = point;
            Normal = normal;
            Damage = damage;
            Shooter = shooter;
            WeaponId = weaponId;
        }

        public ulong TargetId { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float Damage { get; }
        public PlayerKey Shooter { get; }
        public string WeaponId { get; }
    }
}
