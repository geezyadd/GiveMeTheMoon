using Features.NetworkModelModule.Scripts;
using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Configuration;
using Features.ParcelModule.Scripts.Server;
using Features.ShipModule.Scripts;
using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    // A rope hooks only a drifting parcel, at a hit on the crate. The pulled parcel comes at the rope's speed toward its
    // holder's weapon; the rope breaks when the holder lets go, takes another weapon or gets farther than the rope.
    // A parcel that reaches its holder or the deck is let go over the deck and falls onto it as a plain crate again.
    public sealed class ParcelRecoveryService : IParcelRecoveryService {
        private const int NO_CARRIER = 0;

        private readonly ParcelServerModel _parcelServerModel;
        private readonly IParcelEntryService _parcelEntryService;
        private readonly IParcelAboardService _parcelAboardService;
        private readonly IWeaponHolderAnchors _weaponHolderAnchors;
        private readonly ParcelConfiguration _parcelConfiguration;

        public ParcelRecoveryService(
            ParcelServerModel parcelServerModel,
            IParcelEntryService parcelEntryService,
            IParcelAboardService parcelAboardService,
            IWeaponHolderAnchors weaponHolderAnchors,
            ParcelConfiguration parcelConfiguration) {
            _parcelServerModel = parcelServerModel;
            _parcelEntryService = parcelEntryService;
            _parcelAboardService = parcelAboardService;
            _weaponHolderAnchors = weaponHolderAnchors;
            _parcelConfiguration = parcelConfiguration;
        }

        public bool IsHookable(uint netId) =>
            _parcelServerModel.TryGet(netId, out ParcelServerRecord record)
            && record.Crate != null
            && _parcelEntryService.TryFind(netId, out ParcelEntry entry)
            && entry.State == ParcelState.Drifting;

        public void ServerHook(uint netId, in WeaponHit hit, in WeaponPull pull) {
            if (IsHookable(netId) == false || _parcelServerModel.TryGet(netId, out ParcelServerRecord record) == false)
                return;

            float reach = _parcelConfiguration.HitPointToleranceMeters;
            if ((hit.Point - record.Crate.Position).sqrMagnitude > reach * reach)
                return;

            _parcelEntryService.ServerSetState(netId, ParcelState.Pulled, NO_CARRIER, hit.Shooter.Id, hit.WeaponId);
            record.RememberPull(pull.PullSpeedMetersPerSecond, pull.MaxRopeMeters);
            record.RestartStateClock(Time.time);
            record.Crate.SetStrikable(false);
            record.Crate.HoldAt(record.Crate.Position);
        }

        public void ServerPullIn(ParcelServerRecord record, ShipBase ship, float deltaTime) {
            if (_parcelEntryService.TryFind(record.NetId, out ParcelEntry entry) == false || entry.State != ParcelState.Pulled)
                return;

            if (TryGetHeldRopeEnd(record, entry, out Vector3 holder) == false) {
                LetDrift(record);
                return;
            }

            Vector3 position = Vector3.MoveTowards(record.Crate.Position, holder, record.PullSpeed * deltaTime);
            record.Crate.HoldAt(position);
            bool isIn = Vector3.Distance(position, holder) <= _parcelConfiguration.PullArriveMeters || _parcelAboardService.IsAboard(position, ship);
            if (isIn)
                Board(record, ship);
        }

        public void ServerCatchFlownInto(ParcelServerRecord record, ShipBase ship) {
            bool isDrifting = _parcelEntryService.TryFind(record.NetId, out ParcelEntry entry) && entry.State == ParcelState.Drifting;
            if (isDrifting && ship.IsFlying && _parcelAboardService.IsFlownInto(record.Crate.Position, ship))
                Board(record, ship);
        }

        private bool TryGetHeldRopeEnd(ParcelServerRecord record, in ParcelEntry entry, out Vector3 holder) {
            holder = Vector3.zero;
            if (_weaponHolderAnchors.TryGetAnchor(new PlayerKey(entry.HookerId), out WeaponHolderAnchor anchor) == false || anchor.WeaponId != entry.HookWeaponId)
                return false;

            holder = anchor.Origin;
            return (record.Crate.Position - holder).sqrMagnitude <= record.MaxRopeMeters * record.MaxRopeMeters;
        }

        private void LetDrift(ParcelServerRecord record) {
            _parcelEntryService.ServerSetState(record.NetId, ParcelState.Drifting, NO_CARRIER, string.Empty, string.Empty);
            record.RestartStateClock(Time.time);
            record.Crate.Drift(Vector3.zero);
            record.Crate.SetStrikable(true);
        }

        private void Board(ParcelServerRecord record, ShipBase ship) {
            _parcelEntryService.ServerSetState(record.NetId, ParcelState.Loose, NO_CARRIER, string.Empty, string.Empty);
            record.RestartStateClock(Time.time);
            record.Crate.SetStrikable(false);
            record.Crate.Grabbable.SetGrabBlocked(false);
            record.Crate.Settle(_parcelAboardService.CalculateDeckDropPoint(record.Crate.Position, ship));
            if (ship.IsFlying)
                ship.ServerCatchDeckItem(record.Crate.Grabbable);
        }
    }
}
