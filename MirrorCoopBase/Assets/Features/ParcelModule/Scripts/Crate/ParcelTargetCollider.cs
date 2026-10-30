using Features.ParcelModule.Scripts.Services;
using Features.WeaponModule.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ParcelModule.Scripts.Crate {
    // The trigger round the crate a weapon ray strikes while the parcel drifts (layer FieldRock, as every weapon
    // target), with the parcel's weapon target id; the harpoon's rope hooks it there.
    public sealed class ParcelTargetCollider : MonoBehaviour, IWeaponTargetCollider {
        [SerializeField] private NetworkIdentity _identity;

        private IParcelWeaponIds _parcelWeaponIds;

        public ulong TargetId => _parcelWeaponIds.GetTargetId(_identity.netId);

        [Inject]
        private void InjectDependencies(IParcelWeaponIds parcelWeaponIds) =>
            _parcelWeaponIds = parcelWeaponIds;
    }
}
