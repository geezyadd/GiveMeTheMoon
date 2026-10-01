using System.Collections.Generic;
using UnityEngine;

namespace Features.PlayerLifeModule.Scripts.Spectator {
    public interface ISpectatorTargetService {
        public void CollectTargets(IReadOnlyList<Transform> alivePlayerTargets, List<Transform> result);
        public int ResolveIndex(IReadOnlyList<Transform> targets, Transform current, int currentIndex);
        public int Step(int index, int direction, int count);
        public string BuildTargetLabel(Transform target, int index);
        public string BuildNoTargetsLabel();
    }
}
