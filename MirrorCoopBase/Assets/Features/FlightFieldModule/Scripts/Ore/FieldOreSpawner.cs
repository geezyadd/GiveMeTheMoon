using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: a new piece starts its drift now, in field space, and lives its kind's lifetime. No field, no piece.
    public sealed class FieldOreSpawner : IFieldOreSpawner {
        private readonly IFieldOres _fieldOres;
        private readonly IFieldOreWriter _fieldOreWriter;
        private readonly IFieldOreMotion _fieldOreMotion;
        private readonly IFieldOreClock _fieldOreClock;
        private readonly FieldOreIdModel _fieldOreIdModel;

        public FieldOreSpawner(IFieldOres fieldOres, IFieldOreWriter fieldOreWriter, IFieldOreMotion fieldOreMotion, IFieldOreClock fieldOreClock,
                               FieldOreIdModel fieldOreIdModel) {
            _fieldOres = fieldOres;
            _fieldOreWriter = fieldOreWriter;
            _fieldOreMotion = fieldOreMotion;
            _fieldOreClock = fieldOreClock;
            _fieldOreIdModel = fieldOreIdModel;
        }

        public bool TrySpawn(FieldOreDefinition kind, Vector3 worldPosition, Vector3 worldVelocity, out FieldOreId id) {
            id = default;
            if (!_fieldOreWriter.IsAvailable || !_fieldOres.HasField)
                return false;

            double now = _fieldOreClock.Now;
            id = new FieldOreId(_fieldOreIdModel.TakeNextId());
            _fieldOreWriter.ServerAdd(new FieldOreEntry {
                Id = id.Value,
                KindId = kind.Id,
                FieldPosition = _fieldOreMotion.ToFieldPosition(worldPosition),
                FieldVelocity = _fieldOreMotion.ToFieldDirection(worldVelocity),
                StartTime = now,
                ExpiresAt = now + kind.LifetimeSeconds,
                HookerId = string.Empty,
                HookWeaponId = string.Empty,
            });
            return true;
        }
    }
}
