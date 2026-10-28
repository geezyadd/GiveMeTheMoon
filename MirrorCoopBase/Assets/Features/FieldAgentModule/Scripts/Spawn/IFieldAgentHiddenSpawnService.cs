using System.Collections.Generic;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Spawn {
    // Server: a point near a centre where agents can appear without any player seeing it happen.
    public interface IFieldAgentHiddenSpawnService {
        public bool TryFindHiddenPoint(Vector3 center, IReadOnlyList<Vector3> viewpoints, int seed, out Vector3 point, out FieldAgentSpawnCover cover);
        public bool IsHiddenByRock(Vector3 point, IReadOnlyList<Vector3> viewpoints);
        public bool IsHidden(Vector3 point, IReadOnlyList<Vector3> viewpoints);
        public bool IsClearOfRocks(Vector3 point);
    }
}
