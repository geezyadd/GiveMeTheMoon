using Features.WeaponModule.Contracts;

namespace Features.WeaponModule.Scripts.Parts.Effects {
    // What an accepted hit does to its target on the server: damage, a pull, a push. A weapon has a list of them.
    public interface IHitEffect {
        public void Apply(in WeaponHit hit, IWeaponTarget target);
    }
}
