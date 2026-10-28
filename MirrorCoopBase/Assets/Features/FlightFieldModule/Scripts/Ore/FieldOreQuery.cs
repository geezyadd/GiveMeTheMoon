using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.NetworkModelModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Finds the ore of the live field where every piece is now on this peer. A field holds a few dozen pieces at most,
    // so a query walks them all. Allocates nothing per call.
    public sealed class FieldOreQuery : IFieldOreQuery {
        private readonly IFieldOres _fieldOres;
        private readonly IFieldOreMotion _fieldOreMotion;
        private readonly IFieldOreClock _fieldOreClock;
        private readonly FieldOreCatalog _fieldOreCatalog;

        public FieldOreQuery(IFieldOres fieldOres, IFieldOreMotion fieldOreMotion, IFieldOreClock fieldOreClock, FieldOreCatalog fieldOreCatalog) {
            _fieldOres = fieldOres;
            _fieldOreMotion = fieldOreMotion;
            _fieldOreClock = fieldOreClock;
            _fieldOreCatalog = fieldOreCatalog;
        }

        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldOreDescription> results) {
            results.Clear();
            if (!_fieldOres.HasField)
                return;

            FieldRockVolume volume = FieldRockVolume.CreateBox(center, halfExtents, rotation);
            double now = _fieldOreClock.Now;
            for (int i = 0; i < _fieldOres.Count; i++) {
                FieldOreDescription ore = Describe(_fieldOres.Get(i), now);
                if (volume.Intersects(ore.Center, ore.Radius))
                    results.Add(ore);
            }
        }

        public void FindAll(List<FieldOreDescription> results) {
            results.Clear();
            if (!_fieldOres.HasField)
                return;

            double now = _fieldOreClock.Now;
            for (int i = 0; i < _fieldOres.Count; i++)
                results.Add(Describe(_fieldOres.Get(i), now));
        }

        public bool TryGetOre(FieldOreId id, out FieldOreDescription ore) {
            ore = default;
            if (!_fieldOres.HasField || !_fieldOres.TryFindIndex(id.Value, out int index))
                return false;

            ore = Describe(_fieldOres.Get(index), _fieldOreClock.Now);
            return true;
        }

        private FieldOreDescription Describe(in FieldOreEntry entry, double now) {
            FieldOreDefinition kind = _fieldOreCatalog.GetRequired(entry.KindId);
            return new FieldOreDescription(new FieldOreId(entry.Id), kind.Id, _fieldOreMotion.GetWorldPosition(entry, now), kind.RadiusMeters, kind.Price,
                new PlayerKey(entry.HookerId));
        }
    }
}
