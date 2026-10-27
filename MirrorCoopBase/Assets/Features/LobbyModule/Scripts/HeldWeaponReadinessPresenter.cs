using Features.MvpModule;
using Features.WeaponModule.Scripts.Readiness;

namespace Features.LobbyModule.Scripts {
    // The local player's weapon in hand as a cooldown ring around the crosshair, while one is held.
    public sealed class HeldWeaponReadinessPresenter : PresenterBehaviour<HeldWeaponReadinessViewBase> {
        private readonly HeldWeaponReadinessModel _heldWeaponReadinessModel;

        public HeldWeaponReadinessPresenter(HeldWeaponReadinessModel heldWeaponReadinessModel) =>
            _heldWeaponReadinessModel = heldWeaponReadinessModel;

        protected override void OnViewSet() {
            _heldWeaponReadinessModel.OnReadinessChanged += Refresh;
            Refresh();
        }

        protected override void OnDisposed() =>
            _heldWeaponReadinessModel.OnReadinessChanged -= Refresh;

        private void Refresh() {
            View.SetReadinessVisible(_heldWeaponReadinessModel.IsHolding);
            View.SetReadiness(_heldWeaponReadinessModel.Readiness);
        }
    }
}
