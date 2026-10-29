using Features.ParcelModule.Scripts.Services;
using Features.WeaponModule.Contracts;

namespace Features.ParcelModule.Scripts.Server {
    // Server: the drifting parcel a reported hit names, as the weapon hit path sees it. One instance serves every
    // parcel: the hit path aims it and uses it within the same call. A rope hooks it; a bullet does nothing to it.
    public sealed class ParcelPullTarget : IWeaponPullTarget {
        private readonly IParcelRecoveryService _parcelRecoveryService;

        private uint _netId;

        public bool IsAlive => _parcelRecoveryService.IsHookable(_netId);

        public ParcelPullTarget(IParcelRecoveryService parcelRecoveryService) =>
            _parcelRecoveryService = parcelRecoveryService;

        public void AimAt(uint netId) =>
            _netId = netId;

        public void ServerReceivePull(in WeaponHit hit, in WeaponPull pull) =>
            _parcelRecoveryService.ServerHook(_netId, hit, pull);
    }
}
