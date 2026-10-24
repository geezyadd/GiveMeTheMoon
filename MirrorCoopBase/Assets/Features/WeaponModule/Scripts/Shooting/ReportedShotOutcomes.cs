using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Feel;

namespace Features.WeaponModule.Scripts.Shooting {
    // The holding peer shows its shots at once and passes every target hit on to the server.
    public sealed class ReportedShotOutcomes : IShotOutcomeReceiver {
        private readonly IWeaponFeel _feel;
        private readonly IWeaponHitReporter _hitReporter;

        public ReportedShotOutcomes(IWeaponFeel feel, IWeaponHitReporter hitReporter) {
            _feel = feel;
            _hitReporter = hitReporter;
        }

        public void ReceiveOutcome(in WeaponShotOutcome outcome) {
            _feel.PresentShot(outcome);
            if (outcome.IsTargetHit)
                _hitReporter.ReportTargetHit(outcome);
        }
    }
}
