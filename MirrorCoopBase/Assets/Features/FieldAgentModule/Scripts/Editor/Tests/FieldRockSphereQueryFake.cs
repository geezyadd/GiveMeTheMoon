using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Editor.Tests {
    // The field data of a test: a query finds the rocks whose sphere touches the asked sphere, as the field's does.
    public sealed class FieldRockSphereQueryFake : IFieldRockQuery {
        private readonly List<FieldRockDescription> _rocks = new();

        public void Add(Vector3 center, float radius) =>
            _rocks.Add(new FieldRockDescription(FieldRockId.CreateCluster(_rocks.Count, 0), "test_rock", center, radius * 2f, FieldRockSize.Large, false));

        public void FindInSphere(Vector3 center, float radius, List<FieldRockDescription> results) {
            results.Clear();
            foreach (FieldRockDescription rock in _rocks) {
                if (Vector3.Distance(center, rock.Center) <= radius + rock.Radius)
                    results.Add(rock);
            }
        }

        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldRockDescription> results) =>
            FindInSphere(center, halfExtents.magnitude, results);

        public bool TryGetRock(FieldRockId id, out FieldRockDescription rock) {
            foreach (FieldRockDescription candidate in _rocks) {
                if (candidate.Id.Equals(id) == false)
                    continue;

                rock = candidate;
                return true;
            }

            rock = default;
            return false;
        }
    }
}
