using Features.ShipModule.Scripts;
using Features.WeaponModule.Scripts.Definitions;

namespace Features.TurretModule.Editor.Tests {
    // One turret gun the shop sells and the weapon its slot view fires.
    public readonly struct TurretTier {
        public ModuleDefinition Module { get; }
        public WeaponDefinition Weapon { get; }
        public float DamagePerSecond => Weapon.ShotsPerSecond * Weapon.DamagePerHit;

        public TurretTier(ModuleDefinition module, WeaponDefinition weapon) {
            Module = module;
            Weapon = weapon;
        }
    }
}
