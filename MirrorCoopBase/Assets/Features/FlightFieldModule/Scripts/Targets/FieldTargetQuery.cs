using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Server;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Server: finds the target balloons of the current field in a world volume from the generated field, not from the
    // scene (the server builds only the balloons around its own camera). A field holds a few dozen balloons, so a
    // query walks them all; popped balloons are left out. Allocates nothing per call.
    public sealed class FieldTargetQuery : IFieldTargetQuery {
        private readonly IReadOnlyFlightFieldServerModel _flightFieldServerModel;
        private readonly FieldTargetCatalog _fieldTargetCatalog;
        private readonly IFieldPoppedTargets _fieldPoppedTargets;

        public FieldTargetQuery(IReadOnlyFlightFieldServerModel flightFieldServerModel, FieldTargetCatalog fieldTargetCatalog, IFieldPoppedTargets fieldPoppedTargets) {
            _flightFieldServerModel = flightFieldServerModel;
            _fieldTargetCatalog = fieldTargetCatalog;
            _fieldPoppedTargets = fieldPoppedTargets;
        }

        public void FindInSphere(Vector3 center, float radius, List<FieldTargetDescription> results) =>
            Find(FieldRockVolume.CreateSphere(center, radius), results);

        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldTargetDescription> results) =>
            Find(FieldRockVolume.CreateBox(center, halfExtents, rotation), results);

        public bool TryGetTarget(FieldTargetId id, out FieldTargetDescription target) {
            target = default;
            if (!_flightFieldServerModel.HasField || id.Index < 0 || id.Index >= _flightFieldServerModel.Field.Targets.Count || _fieldPoppedTargets.IsPopped(id))
                return false;

            target = Describe(id.Index);
            return true;
        }

        private void Find(FieldRockVolume volume, List<FieldTargetDescription> results) {
            results.Clear();
            if (!_flightFieldServerModel.HasField)
                return;

            IReadOnlyList<FieldTargetPlacement> targets = _flightFieldServerModel.Field.Targets;
            for (int i = 0; i < targets.Count; i++) {
                FieldTargetDescription target = Describe(i);
                if (volume.Intersects(target.Center, target.Radius) && !_fieldPoppedTargets.IsPopped(target.Id))
                    results.Add(target);
            }
        }

        private FieldTargetDescription Describe(int index) {
            FieldTargetPlacement placement = _flightFieldServerModel.Field.Targets[index];
            FieldTargetDefinition kind = _fieldTargetCatalog.GetRequired(placement.KindId);
            FieldFrame frame = _flightFieldServerModel.Frame;
            Vector3 center = frame.Origin + Quaternion.LookRotation(frame.Forward, Vector3.up) * placement.FieldPosition;
            return new FieldTargetDescription(new FieldTargetId(index), kind.Id, center, kind.RadiusMeters, kind.Reward);
        }
    }
}
