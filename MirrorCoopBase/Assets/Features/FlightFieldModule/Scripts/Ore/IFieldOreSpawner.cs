using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: puts one piece of ore of a kind into the live field, drifting from a world point at a world velocity.
    public interface IFieldOreSpawner {
        public bool TrySpawn(FieldOreDefinition kind, Vector3 worldPosition, Vector3 worldVelocity, out FieldOreId id);
    }
}
