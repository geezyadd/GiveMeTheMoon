using System.Collections.Generic;
using UnityEngine;

namespace Features.TargetModule {
    // The targets that are on, on this peer, and the queries over them.
    public sealed class TargetRegistry {
        private readonly List<Target> _targets = new();
        private readonly List<Target> _candidates = new();
        private readonly List<Vector3> _candidatePoints = new();

        public IReadOnlyList<Target> Targets => _targets;

        public void Add(Target target) {
            if (target != null && _targets.Contains(target) == false)
                _targets.Add(target);
        }

        public void Remove(Target target) =>
            _targets.Remove(target);

        // The nearest available target of this kind in front of the seeker (TargetPickRule.PickNearest), other than
        // the seeker's own; null when there is none.
        public Target PickNearest(TargetKind kind, Vector3 origin, Vector3 forward, float maxDistance, float maxAngle,
            Target own = null) {
            _candidates.Clear();
            _candidatePoints.Clear();
            foreach (Target target in _targets) {
                if (target == null || target == own || target.Is(kind) == false || target.IsAvailable == false)
                    continue;

                _candidates.Add(target);
                _candidatePoints.Add(target.AimPoint.position);
            }

            int picked = TargetPickRule.PickNearest(origin, forward, _candidatePoints, maxDistance, maxAngle);
            return picked >= 0 ? _candidates[picked] : null;
        }

        // The available target of this kind that owns this rigidbody, other than the seeker's own; null for anything
        // else.
        public Target FindByPart(TargetKind kind, Rigidbody part, Target own = null) {
            if (part == null)
                return null;

            foreach (Target target in _targets)
                if (target != null && target != own && target.Is(kind) && target.IsAvailable && target.Owns(part))
                    return target;

            return null;
        }
    }
}
