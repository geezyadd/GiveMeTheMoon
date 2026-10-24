using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Server: the damage the rocks of the current field have taken, only for the rocks hit and not broken yet, so a
    // field of thousands of rocks keeps no entry per rock. Not synced: peers learn only that a rock broke.
    public sealed class FieldRockDamageModel : IReadOnlyFieldRockDamageModel {
        private readonly Dictionary<FieldRockId, float> _damageTaken = new();

        public int DamagedCount => _damageTaken.Count;

        public float GetDamageTaken(FieldRockId id) =>
            _damageTaken.TryGetValue(id, out float damage) ? damage : 0f;

        public void SetDamageTaken(FieldRockId id, float damage) =>
            _damageTaken[id] = damage;

        public void Forget(FieldRockId id) =>
            _damageTaken.Remove(id);

        public void Clear() =>
            _damageTaken.Clear();
    }
}
