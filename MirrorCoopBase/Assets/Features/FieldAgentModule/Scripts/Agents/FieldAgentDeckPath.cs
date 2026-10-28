using UnityEngine;
using UnityEngine.AI;

namespace Features.FieldAgentModule.Scripts.Agents {
    // An agent's cached walk over the deck NavMesh, ship-local corners. One per model slot, reused, so a repath
    // allocates nothing.
    public sealed class FieldAgentDeckPath {
        private const int MAX_CORNERS = 16;

        public NavMeshPath NavMeshPath { get; } = new();
        public Vector3[] Corners { get; } = new Vector3[MAX_CORNERS];
        public int CornerCount { get; private set; }
        public int NextCorner { get; private set; }
        public Vector3 Goal { get; private set; }
        public float Age { get; private set; }
        public bool HasPath => CornerCount > 0;
        public bool IsAtEnd => NextCorner >= CornerCount;

        public void SetCorners(int count, Vector3 goal) {
            CornerCount = Mathf.Min(count, MAX_CORNERS);
            NextCorner = CornerCount > 1 ? 1 : 0;
            Goal = goal;
            Age = 0f;
        }

        public void PassCorner() =>
            NextCorner++;

        public void AddAge(float seconds) =>
            Age += seconds;

        public void Clear() {
            CornerCount = 0;
            NextCorner = 0;
            Age = 0f;
        }
    }
}
