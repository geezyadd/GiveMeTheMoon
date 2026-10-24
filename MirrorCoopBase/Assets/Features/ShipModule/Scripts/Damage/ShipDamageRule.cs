using System.Collections.Generic;
using UnityEngine;

namespace Features.ShipModule.Scripts.Damage {
    // What one rock impact damages. A module: the installed breakable module nearest to the hit within the search
    // radius, with the configured chance x impact strength x the module's multiplier. A breach: the nearest free hull
    // damage point, with the configured chance x strength, while fewer than the limit are open. The roll is in [0, 1).
    public sealed class ShipDamageRule : IShipDamageRule {
        private readonly ShipDamageConfiguration _shipDamageConfiguration;

        public ShipDamageRule(ShipDamageConfiguration shipDamageConfiguration) =>
            _shipDamageConfiguration = shipDamageConfiguration;

        public bool TryPickBrokenModule(IReadOnlyList<ShipDamageCandidate> modules, Vector3 point, float strength, float roll, out string socketId) {
            socketId = null;
            if (TryFindNearest(modules, point, _shipDamageConfiguration.ModuleSearchRadius, out ShipDamageCandidate nearest) == false)
                return false;

            if (IsRollWithin(roll, _shipDamageConfiguration.ModuleBreakChance * strength * nearest.ChanceMultiplier) == false)
                return false;

            socketId = nearest.Id;
            return true;
        }

        public bool TryPickBreach(IReadOnlyList<ShipDamageCandidate> points, int openBreaches, Vector3 point, float strength, float roll, out string pointId) {
            pointId = null;
            if (openBreaches >= _shipDamageConfiguration.MaxOpenBreaches)
                return false;

            if (TryFindNearest(points, point, float.PositiveInfinity, out ShipDamageCandidate nearest) == false)
                return false;

            if (IsRollWithin(roll, _shipDamageConfiguration.BreachChance * strength * nearest.ChanceMultiplier) == false)
                return false;

            pointId = nearest.Id;
            return true;
        }

        private static bool IsRollWithin(float roll, float chance) =>
            roll < Mathf.Clamp01(chance);

        private static bool TryFindNearest(IReadOnlyList<ShipDamageCandidate> candidates, Vector3 point, float radius, out ShipDamageCandidate nearest) {
            nearest = default;
            bool isFound = false;
            float nearestSqr = radius * radius;
            for (int i = 0; i < candidates.Count; i++) {
                ShipDamageCandidate candidate = candidates[i];
                float distanceSqr = (candidate.Position - point).sqrMagnitude;
                if (candidate.IsDamageable == false || distanceSqr > nearestSqr)
                    continue;

                nearest = candidate;
                nearestSqr = distanceSqr;
                isFound = true;
            }

            return isFound;
        }
    }
}
