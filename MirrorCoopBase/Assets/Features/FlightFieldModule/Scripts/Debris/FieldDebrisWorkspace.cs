using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Debris {
    // A caller's scratch state for the debris layout: the grid points already drawn and already decided (every point is
    // drawn and decided once however many cells look at it) and the random sequence they are drawn with. One per field
    // and per thread.
    public sealed class FieldDebrisWorkspace {
        private readonly Dictionary<Vector2Int, FieldDebrisCandidate> _candidates = new();
        private readonly Dictionary<Vector2Int, bool> _isKept = new();

        public IFieldRandom Random { get; }
        // The points waiting for their rivals to be decided; kept here so deciding allocates nothing.
        public Stack<Vector2Int> Pending { get; } = new();

        public FieldDebrisWorkspace(IFieldRandom random) =>
            Random = random;

        public bool TryGet(Vector2Int point, out FieldDebrisCandidate candidate) =>
            _candidates.TryGetValue(point, out candidate);

        public void Remember(Vector2Int point, FieldDebrisCandidate candidate) =>
            _candidates[point] = candidate;

        public bool TryGetKept(Vector2Int point, out bool isKept) =>
            _isKept.TryGetValue(point, out isKept);

        public void RememberKept(Vector2Int point, bool isKept) =>
            _isKept[point] = isKept;

        public void Clear() {
            _candidates.Clear();
            _isKept.Clear();
        }
    }
}
