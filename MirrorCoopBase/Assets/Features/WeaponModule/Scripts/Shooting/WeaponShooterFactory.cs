using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Feel;

namespace Features.WeaponModule.Scripts.Shooting {
    // Builds a weapon's runtime parts from its definition: every shooter gets its own fire mode and delivery.
    public sealed class WeaponShooterFactory : IWeaponShooterFactory {
        private readonly IShotDeliveryContext _shotDeliveryContext;

        public WeaponShooterFactory(IShotDeliveryContext shotDeliveryContext) =>
            _shotDeliveryContext = shotDeliveryContext;

        public WeaponShooter Create(WeaponDefinition weapon, IWeaponFeel feel, IShotOutcomeReceiver outcomeReceiver) =>
            new WeaponShooter(
                weapon,
                weapon.FireMode.CreateFireMode(weapon.ShotsPerSecond),
                weapon.Delivery.CreateDelivery(_shotDeliveryContext),
                feel,
                outcomeReceiver);
    }
}
