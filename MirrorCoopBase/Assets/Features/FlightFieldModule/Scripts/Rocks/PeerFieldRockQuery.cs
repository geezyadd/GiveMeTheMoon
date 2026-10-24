using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // The rock query over the published field: the server's FieldRockQuery with the live field as its source, so both
    // share one reader and one set of layouts.
    public sealed class PeerFieldRockQuery : IPeerFieldRockQuery {
        private readonly IFieldRockQuery _fieldRockQuery;

        public PeerFieldRockQuery(IInstantiator instantiator) =>
            _fieldRockQuery = instantiator.Instantiate<FieldRockQuery>(new object[] { instantiator.Instantiate<LiveFieldRockFieldSource>() });

        public void FindInSphere(Vector3 center, float radius, List<FieldRockDescription> results) =>
            _fieldRockQuery.FindInSphere(center, radius, results);

        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldRockDescription> results) =>
            _fieldRockQuery.FindInBox(center, halfExtents, rotation, results);

        public bool TryGetRock(FieldRockId id, out FieldRockDescription rock) =>
            _fieldRockQuery.TryGetRock(id, out rock);
    }
}
