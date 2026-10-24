using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Features.SonarModule.Editor.Tests {
    // A field of rocks for a test: the query finds what touches the volume, a broken rock is left out as the real
    // query does.
    public sealed class FakeFieldRockQuery : IFieldRockQuery {
        private readonly List<FieldRockDescription> _rocks = new();

        public void Add(FieldRockDescription rock) =>
            _rocks.Add(rock);

        public void Break(FieldRockId id) =>
            _rocks.RemoveAll(rock => rock.Id.Equals(id));

        public void FindInSphere(Vector3 center, float radius, List<FieldRockDescription> results) {
            results.Clear();
            foreach (FieldRockDescription rock in _rocks) {
                if (Vector3.Distance(rock.Center, center) <= radius + rock.Radius)
                    results.Add(rock);
            }
        }

        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldRockDescription> results) {
            results.Clear();
            Bounds box = new Bounds(center, halfExtents * 2f);
            foreach (FieldRockDescription rock in _rocks) {
                if (box.SqrDistance(rock.Center) <= rock.Radius * rock.Radius)
                    results.Add(rock);
            }
        }

        public bool TryGetRock(FieldRockId id, out FieldRockDescription rock) {
            rock = default;
            foreach (FieldRockDescription candidate in _rocks) {
                if (candidate.Id.Equals(id) == false)
                    continue;

                rock = candidate;
                return true;
            }

            return false;
        }
    }
}
