using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Features.SonarModule.Editor.Tests {
    // Balloons for a test: the query finds what touches the volume, a popped balloon is left out as the real query does.
    public sealed class FakeFieldTargetQuery : IPeerFieldTargetQuery {
        private readonly List<FieldTargetDescription> _targets = new();

        public void Add(FieldTargetDescription target) =>
            _targets.Add(target);

        public void Pop(FieldTargetId id) =>
            _targets.RemoveAll(target => target.Id.Equals(id));

        public void FindInSphere(Vector3 center, float radius, List<FieldTargetDescription> results) {
            results.Clear();
            foreach (FieldTargetDescription target in _targets) {
                if (Vector3.Distance(target.Center, center) <= radius + target.Radius)
                    results.Add(target);
            }
        }

        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldTargetDescription> results) {
            results.Clear();
            Bounds box = new Bounds(center, halfExtents * 2f);
            foreach (FieldTargetDescription target in _targets) {
                if (box.SqrDistance(target.Center) <= target.Radius * target.Radius)
                    results.Add(target);
            }
        }

        public bool TryGetTarget(FieldTargetId id, out FieldTargetDescription target) {
            target = default;
            foreach (FieldTargetDescription candidate in _targets) {
                if (candidate.Id.Equals(id) == false)
                    continue;

                target = candidate;
                return true;
            }

            return false;
        }
    }
}
