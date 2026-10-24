using Features.WeaponModule.Scripts.Parts.Delivery;

namespace Features.WeaponModule.Scripts.Shooting {
    // Where a shooter's target hits go: the holding peer sends them to the server, every other peer only shows them.
    public interface IWeaponHitReporter {
        public void ReportTargetHit(in WeaponShotOutcome outcome);
    }
}
