using System.Collections.Generic;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Spawn {
    // Server: where every connected player looks from now.
    public interface IFieldAgentViewpointSource {
        public void CollectViewpoints(List<Vector3> results);
    }
}
