using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Targets {
    // The target query over the published field: the server's FieldTargetQuery with the live field as its source.
    public sealed class PeerFieldTargetQuery : IPeerFieldTargetQuery {
        private readonly IFieldTargetQuery _fieldTargetQuery;

        public PeerFieldTargetQuery(IInstantiator instantiator) =>
            _fieldTargetQuery = instantiator.Instantiate<FieldTargetQuery>(new object[] { instantiator.Instantiate<LiveFieldTargetFieldSource>() });

        public void FindInSphere(Vector3 center, float radius, List<FieldTargetDescription> results) =>
            _fieldTargetQuery.FindInSphere(center, radius, results);

        public void FindInBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, List<FieldTargetDescription> results) =>
            _fieldTargetQuery.FindInBox(center, halfExtents, rotation, results);

        public bool TryGetTarget(FieldTargetId id, out FieldTargetDescription target) =>
            _fieldTargetQuery.TryGetTarget(id, out target);
    }
}
