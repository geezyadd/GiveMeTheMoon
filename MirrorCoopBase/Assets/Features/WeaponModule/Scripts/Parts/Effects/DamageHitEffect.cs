using System;
using Features.WeaponModule.Contracts;

namespace Features.WeaponModule.Scripts.Parts.Effects {
    // Hands the hit with the weapon's damage to a target that takes damage (IWeaponDamageTarget), which subtracts it
    // from its own durability; any other target (field ore) is not damaged.
    [Serializable]
    public sealed class DamageHitEffect : IHitEffect {
        public void Apply(in WeaponHit hit, IWeaponTarget target) {
            if (target is IWeaponDamageTarget damageTarget)
                damageTarget.ServerReceiveHit(hit);
        }
    }
}
