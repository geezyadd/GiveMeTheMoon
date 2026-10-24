using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Feel;

namespace Features.WeaponModule.Scripts.Shooting {
    public interface IWeaponShooterFactory {
        public WeaponShooter Create(WeaponDefinition weapon, IWeaponFeel feel, IShotOutcomeReceiver outcomeReceiver);
    }
}
