using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Rocks;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Finds the target balloons of the current field in a world volume from the field data, not from the scene (a peer
    // builds only the balloons around its own camera): the server reads its generated field, every peer the published
    // one (IFieldTargetFieldSource). A field holds a few dozen balloons, so a
    // query walks them all; popped balloons are left out. Allocates nothing per call.
    public sealed class FieldTargetQuery : IFieldTargetQuery {
        private readonly IFieldTargetFieldSource _fieldTargetFieldSource;
        private readonly FieldTargetCatalog _fieldTargetCatalog;
        private readonly IFieldPoppedTargets _fieldPoppedTargets;

        public FieldTargetQuery(IFieldTargetFieldSource fieldTargetFieldSource, FieldTargetCatalog fieldTargetCatalog, IFieldPoppedTargets fieldPoppedTargets) {
            _fieldTargetFieldSource = fieldTargetFieldSource;
            _fieldTargetCatalog = fieldTargetCatalog;
            _fieldPoppedTargets = fieldPoppedTargets;
        }

        public void FindInSphere(Vector3 center, float radius, List<FieldTargetDescription> results) =>
            Find(FieldRockVolume.CreateSphere(center, radius), results);

        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldTargetDescription> results) =>
            Find(FieldRockVolume.CreateBox(center, halfExtents, rotation), results);

        public bool TryGetTarget(FieldTargetId id, out FieldTargetDescription target) {
            target = default;
            if (!_fieldTargetFieldSource.HasField || id.Index < 0 || id.Index >= _fieldTargetFieldSource.TargetCount || _fieldPoppedTargets.IsPopped(id))
                return false;

            target = Describe(id.Index);
            return true;
        }

        private void Find(FieldRockVolume volume, List<FieldTargetDescription> results) {
            results.Clear();
            if (!_fieldTargetFieldSource.HasField)
                return;

            int count = _fieldTargetFieldSource.TargetCount;
            for (int i = 0; i < count; i++) {
                FieldTargetDescription target = Describe(i);
                if (volume.Intersects(target.Center, target.Radius) && !_fieldPoppedTargets.IsPopped(target.Id))
                    results.Add(target);
            }
        }

        private FieldTargetDescription Describe(int index) {
            FieldTargetPlacement placement = _fieldTargetFieldSource.GetTarget(index);
            FieldTargetDefinition kind = _fieldTargetCatalog.GetRequired(placement.KindId);
            FieldFrame frame = _fieldTargetFieldSource.Frame;
            Vector3 center = frame.Origin + Quaternion.LookRotation(frame.Forward, Vector3.up) * placement.FieldPosition;
            return new FieldTargetDescription(new FieldTargetId(index), kind.Id, center, kind.RadiusMeters, kind.Reward);
        }
    }
}
