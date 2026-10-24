using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Shooting;

namespace Features.WeaponModule.Editor.Tests {
    public sealed class RecordingHitReporter : IWeaponHitReporter {
        public int Reports { get; private set; }

        public void ReportTargetHit(in WeaponShotOutcome outcome) =>
            Reports++;
    }
}
