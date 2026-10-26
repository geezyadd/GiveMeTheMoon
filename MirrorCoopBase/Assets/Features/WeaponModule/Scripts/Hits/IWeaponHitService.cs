using Features.WeaponModule.Scripts.Network;

namespace Features.WeaponModule.Scripts.Hits {
    public interface IWeaponHitService {
        public WeaponHitVerdict ServerApplyReportedHit(ServerWeaponHolding holding, int reportedWeaponIndex, in WeaponHitReport report, double serverTimeSeconds);
    }
}
