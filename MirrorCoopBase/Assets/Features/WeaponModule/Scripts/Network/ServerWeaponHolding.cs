using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Hits;
using Features.WeaponModule.Scripts.Parts.Holding;

namespace Features.WeaponModule.Scripts.Network {
    // What the server itself knows about one player holding one weapon; a hit report is checked against this, never
    // against the report's own claims. A new holding starts with a fresh fire rate budget.
    public sealed class ServerWeaponHolding {
        public WeaponDefinition Weapon { get; }
        public int WeaponIndex { get; }
        public PlayerKey Shooter { get; }
        public IWeaponAim Aim { get; }
        public WeaponShotBudget Budget { get; } = new WeaponShotBudget();

        public ServerWeaponHolding(WeaponDefinition weapon, int weaponIndex, PlayerKey shooter, IWeaponAim aim) {
            Weapon = weapon;
            WeaponIndex = weaponIndex;
            Shooter = shooter;
            Aim = aim;
        }
    }
}
