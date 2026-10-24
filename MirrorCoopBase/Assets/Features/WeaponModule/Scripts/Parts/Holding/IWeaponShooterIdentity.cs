using Features.NetworkModelModule.Scripts;

namespace Features.WeaponModule.Scripts.Parts.Holding {
    // Who fires a held weapon, as the server knows it: the key every accepted hit carries.
    public interface IWeaponShooterIdentity {
        public PlayerKey Shooter { get; }
    }
}
