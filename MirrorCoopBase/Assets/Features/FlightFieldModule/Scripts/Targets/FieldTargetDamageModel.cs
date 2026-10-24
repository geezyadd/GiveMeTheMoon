using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Server: the weapon damage the target balloons of the current field have taken, only for the balloons hit and not
    // popped yet. Not synced: peers learn only that a balloon popped.
    public sealed class FieldTargetDamageModel {
        private readonly Dictionary<FieldTargetId, float> _damageTaken = new();

        public float GetDamageTaken(FieldTargetId id) =>
            _damageTaken.TryGetValue(id, out float damage) ? damage : 0f;

        public void SetDamageTaken(FieldTargetId id, float damage) =>
            _damageTaken[id] = damage;

        public void Forget(FieldTargetId id) =>
            _damageTaken.Remove(id);

        public void Clear() =>
            _damageTaken.Clear();
    }
}
