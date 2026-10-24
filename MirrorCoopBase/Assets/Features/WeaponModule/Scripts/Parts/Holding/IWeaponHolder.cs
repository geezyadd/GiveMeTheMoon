using Features.NetworkModelModule.Scripts;

namespace Features.WeaponModule.Scripts.Parts.Holding {
    // Who holds the weapon and where its trigger and aim come from: a player seated in a turret, a player with the gun
    // in hand. The weapon core never asks what kind of holder it is.
    public interface IWeaponHolder {
        public PlayerKey Holder { get; }
        public bool IsTriggerHeld { get; }
        public IWeaponAim Aim { get; }
    }
}
