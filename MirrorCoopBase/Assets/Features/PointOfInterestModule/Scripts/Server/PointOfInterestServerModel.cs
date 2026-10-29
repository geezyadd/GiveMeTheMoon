using System.Collections.Generic;

namespace Features.PointOfInterestModule.Scripts.Server {
    // The server's side of the points of interest: the field they were placed for and the damage each reward
    // container took. Not synced: peers see the points' states.
    public sealed class PointOfInterestServerModel {
        private readonly Dictionary<int, float> _damageTaken = new();

        public int PlacedFieldNumber { get; private set; }

        public void SetPlacedField(int fieldNumber) {
            PlacedFieldNumber = fieldNumber;
            _damageTaken.Clear();
        }

        public float AddDamage(int pointId, float damage) {
            _damageTaken.TryGetValue(pointId, out float taken);
            taken += damage;
            _damageTaken[pointId] = taken;
            return taken;
        }
    }
}
