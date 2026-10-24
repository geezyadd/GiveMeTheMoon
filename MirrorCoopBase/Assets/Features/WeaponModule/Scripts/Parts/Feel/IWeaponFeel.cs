using Features.WeaponModule.Scripts.Parts.Delivery;

namespace Features.WeaponModule.Scripts.Parts.Feel {
    // How a weapon looks and feels on one peer (muzzle flash, casings, recoil, shake, tracers, impacts), driven by its
    // WeaponFeelProfile. Client-only and local: nothing here goes over the network.
    public interface IWeaponFeel {
        public void PresentFiring(bool isFiring, float readiness);

        public void PresentShot(in WeaponShotOutcome outcome);
    }
}
