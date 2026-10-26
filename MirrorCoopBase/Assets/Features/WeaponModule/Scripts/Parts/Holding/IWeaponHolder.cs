namespace Features.WeaponModule.Scripts.Parts.Holding {
    // Where a held weapon's trigger and aim come from on a peer: a player seated in a turret, a player with the gun in
    // hand. Who shoots is the server's business (IWeaponShooterIdentity). The core never asks what kind of holder it is.
    public interface IWeaponHolder {
        public bool IsTriggerHeld { get; }
        public IWeaponAim Aim { get; }
    }
}
