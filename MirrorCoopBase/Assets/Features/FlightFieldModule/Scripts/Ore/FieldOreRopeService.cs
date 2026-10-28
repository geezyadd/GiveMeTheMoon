using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network;
using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    public sealed class FieldOreRopeService : IFieldOreRopeService {
        private readonly IFieldOres _fieldOres;
        private readonly IFieldOreMotion _fieldOreMotion;
        private readonly IFieldOreClock _fieldOreClock;
        private readonly IFieldOreHooks _fieldOreHooks;
        private readonly IFieldOreCollector _fieldOreCollector;
        private readonly IWeaponHolderAnchors _weaponHolderAnchors;

        public FieldOreRopeService(IFieldOres fieldOres, IFieldOreMotion fieldOreMotion, IFieldOreClock fieldOreClock, IFieldOreHooks fieldOreHooks,
                                   IFieldOreCollector fieldOreCollector, IWeaponHolderAnchors weaponHolderAnchors) {
            _fieldOres = fieldOres;
            _fieldOreMotion = fieldOreMotion;
            _fieldOreClock = fieldOreClock;
            _fieldOreHooks = fieldOreHooks;
            _fieldOreCollector = fieldOreCollector;
            _weaponHolderAnchors = weaponHolderAnchors;
        }

        // Backwards: taking a piece in removes it from the list.
        public void ServerUpdateRopes() {
            if (!_fieldOres.HasField)
                return;

            double now = _fieldOreClock.Now;
            for (int i = _fieldOres.Count - 1; i >= 0; i--) {
                FieldOreEntry entry = _fieldOres.Get(i);
                if (string.IsNullOrEmpty(entry.HookerId))
                    continue;

                if (_fieldOreMotion.IsPullComplete(entry, now))
                    _fieldOreCollector.ServerCollect(new FieldOreId(entry.Id));
                else if (!IsRopeHeld(entry, now))
                    _fieldOreHooks.ServerRelease(new FieldOreId(entry.Id));
            }
        }

        private bool IsRopeHeld(in FieldOreEntry entry, double now) {
            if (!_weaponHolderAnchors.TryGetAnchor(new PlayerKey(entry.HookerId), out WeaponHolderAnchor anchor) || anchor.WeaponId != entry.HookWeaponId)
                return false;

            float reach = entry.MaxRopeMeters;
            return (_fieldOreMotion.GetWorldPosition(entry, now) - anchor.Origin).sqrMagnitude <= reach * reach;
        }
    }
}
