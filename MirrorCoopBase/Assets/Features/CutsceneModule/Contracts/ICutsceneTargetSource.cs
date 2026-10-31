using UnityEngine;

namespace Features.CutsceneModule.Contracts {
    // Every peer: where a follow shot's target stands on this peer now. A source answers only for the ids it gives out.
    public interface ICutsceneTargetSource {
        public bool TryGetTargetPosition(string targetId, out Vector3 position);
    }
}
