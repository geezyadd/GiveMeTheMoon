using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts.Editor.Tests {
    // The field data of a test: every query finds all of its rocks, whatever the volume.
    public sealed class FieldRockQueryStub : IFieldRockQuery {
        private readonly List<FieldRockDescription> _rocks = new();

        public void Add(FieldRockDescription rock) =>
            _rocks.Add(rock);

        public void FindInSphere(Vector3 center, float radius, List<FieldRockDescription> results) =>
            Fill(results);

        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldRockDescription> results) =>
            Fill(results);

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

        private void Fill(List<FieldRockDescription> results) {
            results.Clear();
            results.AddRange(_rocks);
        }
    }
}
