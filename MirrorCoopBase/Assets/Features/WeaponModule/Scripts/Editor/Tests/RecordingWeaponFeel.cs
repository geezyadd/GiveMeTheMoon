using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Feel;

namespace Features.WeaponModule.Editor.Tests {
    public sealed class RecordingWeaponFeel : IWeaponFeel {
        public bool IsFiring { get; private set; }
        public int Shots { get; private set; }

        public void PresentFiring(bool isFiring, float readiness) =>
            IsFiring = isFiring;

        public void PresentShot(in WeaponShotOutcome outcome) =>
            Shots++;
    }
}
