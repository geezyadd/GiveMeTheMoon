using Features.WeaponModule.Scripts.Definitions;

namespace Features.WeaponModule.Scripts.Shooting {
    public interface IWeaponShooterFactory {
        public WeaponShooter Create(WeaponDefinition weapon);
    }
}
