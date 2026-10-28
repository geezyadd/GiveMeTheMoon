using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network;
using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: the piece is checked against where it is now, not the report: it exists, nobody pulls it yet and the
    // reported hit lies on it. A hooked piece starts its pull from where it is; a released one keeps its drift
    // velocity and its end of life.
    public sealed class FieldOreHookService : IFieldOreHooks {
        private readonly IFieldOres _fieldOres;
        private readonly IFieldOreWriter _fieldOreWriter;
        private readonly IFieldOreMotion _fieldOreMotion;
        private readonly IFieldOreClock _fieldOreClock;
        private readonly FieldOreCatalog _fieldOreCatalog;
        private readonly FieldOreConfiguration _fieldOreConfiguration;

        public FieldOreHookService(IFieldOres fieldOres, IFieldOreWriter fieldOreWriter, IFieldOreMotion fieldOreMotion, IFieldOreClock fieldOreClock,
                                   FieldOreCatalog fieldOreCatalog, FieldOreConfiguration fieldOreConfiguration) {
            _fieldOres = fieldOres;
            _fieldOreWriter = fieldOreWriter;
            _fieldOreMotion = fieldOreMotion;
            _fieldOreClock = fieldOreClock;
            _fieldOreCatalog = fieldOreCatalog;
            _fieldOreConfiguration = fieldOreConfiguration;
        }

        public FieldOreHookResult ServerHook(FieldOreId id, PlayerKey hooker, string weaponId, Vector3 hitPoint, in WeaponPull pull) {
            if (!_fieldOreWriter.IsAvailable || !_fieldOres.HasField)
                return FieldOreHookResult.NoField;

            if (!_fieldOres.TryFindIndex(id.Value, out int index))
                return FieldOreHookResult.NotFound;

            FieldOreEntry entry = _fieldOres.Get(index);
            if (!string.IsNullOrEmpty(entry.HookerId))
                return FieldOreHookResult.AlreadyHooked;

            double now = _fieldOreClock.Now;
            Vector3 position = _fieldOreMotion.GetWorldPosition(entry, now);
            if (!IsOnOre(entry, position, hitPoint))
                return FieldOreHookResult.TooFar;

            if (!_fieldOreMotion.TryGetShipOffset(position, out Vector3 shipOffset))
                return FieldOreHookResult.NoShip;

            ReleaseHeldBy(hooker);
            entry.HookerId = hooker.Id;
            entry.HookWeaponId = weaponId;
            entry.ShipOffset = shipOffset;
            entry.PullSpeed = pull.PullSpeedMetersPerSecond;
            entry.MaxRopeMeters = pull.MaxRopeMeters;
            entry.StartTime = now;
            _fieldOreWriter.ServerSet(index, entry);
            return FieldOreHookResult.Hooked;
        }

        public void ServerRelease(FieldOreId id) {
            if (!_fieldOreWriter.IsAvailable || !_fieldOres.TryFindIndex(id.Value, out int index))
                return;

            FieldOreEntry entry = _fieldOres.Get(index);
            if (string.IsNullOrEmpty(entry.HookerId))
                return;

            double now = _fieldOreClock.Now;
            entry.FieldPosition = _fieldOreMotion.ToFieldPosition(_fieldOreMotion.GetWorldPosition(entry, now));
            entry.StartTime = now;
            entry.HookerId = string.Empty;
            entry.HookWeaponId = string.Empty;
            _fieldOreWriter.ServerSet(index, entry);
        }

        private void ReleaseHeldBy(PlayerKey hooker) {
            for (int i = 0; i < _fieldOres.Count; i++) {
                FieldOreEntry entry = _fieldOres.Get(i);
                if (entry.HookerId == hooker.Id)
                    ServerRelease(new FieldOreId(entry.Id));
            }
        }

        private bool IsOnOre(in FieldOreEntry entry, Vector3 position, Vector3 hitPoint) {
            float reach = _fieldOreCatalog.GetRequired(entry.KindId).RadiusMeters + _fieldOreConfiguration.HitPointToleranceMeters;
            return (hitPoint - position).sqrMagnitude <= reach * reach;
        }
    }
}
