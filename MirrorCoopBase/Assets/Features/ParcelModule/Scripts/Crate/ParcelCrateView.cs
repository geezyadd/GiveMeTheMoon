using Features.FieldAgentModule.Scripts.Client;
using Features.NetworkModelModule.Scripts;
using Features.ParcelModule.Data;
using Features.ParcelModule.Data.Generated;
using Features.ParcelModule.Scripts.Configuration;
using Features.WeaponModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.ParcelModule.Scripts.Crate {
    // Every peer with a client shows the crate by its synced parcel state: no hand takes it unless it lies loose, it
    // hangs under its thief's look (shown 0.25 s behind the server, as every field agent), a weapon ray can strike
    // it only while it drifts, and a rope runs from its hooker's weapon to it while it is pulled in.
    public sealed class ParcelCrateView : MonoBehaviour {
        [SerializeField] private ParcelCrate _crate;

        private IReadOnlyParcelsModel _parcelsModel;
        private IFieldAgentShownAgents _fieldAgentShownAgents;
        private IWeaponHolderAnchors _weaponHolderAnchors;
        private ParcelConfiguration _parcelConfiguration;
        private ParcelState _shownState;

        [Inject]
        private void InjectDependencies(
            IReadOnlyParcelsModel parcelsModel,
            IFieldAgentShownAgents fieldAgentShownAgents,
            IWeaponHolderAnchors weaponHolderAnchors,
            ParcelConfiguration parcelConfiguration) {
            _parcelsModel = parcelsModel;
            _fieldAgentShownAgents = fieldAgentShownAgents;
            _weaponHolderAnchors = weaponHolderAnchors;
            _parcelConfiguration = parcelConfiguration;
        }

        private void LateUpdate() {
            if (_crate.Identity.isClient == false || TryFindEntry(out ParcelEntry entry) == false)
                return;

            if (entry.State != _shownState)
                ShowState(entry.State);

            if (entry.State == ParcelState.Carried)
                FollowThief(entry.CarrierAgentId);

            if (entry.State == ParcelState.Pulled && _weaponHolderAnchors.TryGetAnchor(new PlayerKey(entry.HookerId), out WeaponHolderAnchor anchor))
                _crate.ShowRope(anchor.Origin);
            else
                _crate.HideRope();
        }

        private void ShowState(ParcelState state) {
            bool wasCarried = _shownState == ParcelState.Carried;
            _shownState = state;
            _crate.Grabbable.SetGrabBlocked(state != ParcelState.Loose);
            _crate.SetStrikable(state == ParcelState.Drifting);
            if (state == ParcelState.Carried)
                _crate.SetNetworkBodySynced(false);
            else if (wasCarried)
                _crate.ReleaseFromThiefView();
        }

        private void FollowThief(int carrierAgentId) {
            if (_fieldAgentShownAgents.TryGetShownWorldPosition(carrierAgentId, out Vector3 thief))
                _crate.HoldAt(thief + Vector3.down * _parcelConfiguration.CarrierHangMeters);
        }

        private bool TryFindEntry(out ParcelEntry entry) {
            uint netId = _crate.Identity.netId;
            foreach (ParcelEntry candidate in _parcelsModel.Entries) {
                if (candidate.NetId != netId)
                    continue;

                entry = candidate;
                return true;
            }

            entry = default;
            return false;
        }
    }
}
