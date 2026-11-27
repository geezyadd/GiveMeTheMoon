using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Feel;

namespace Features.WeaponModule.Scripts.Core {
    // A weapon a player can hold: a gun in the hand, a turret on the ship. What it can do besides firing (reload,
    // later aim down sights or charge) is a capability asked for with TryGetCapability, so a new weapon or feature
    // plugs in without a change to the service, the system or the network path.
    public interface IWeapon {
        public WeaponDefinition Definition { get; }
        public IWeaponFeelPresenter Feel { get; }

        // False while it cannot fire at all (a broken turret module).
        public bool IsUsable { get; }

        public bool TryGetCapability<TCapability>(out TCapability capability) where TCapability : class;
    }
}
