using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Cargo;
using Features.ShipDamageModule.Scripts.Damage;
using UnityEngine;

namespace Features.BugModule.Scripts.Brain {
    // What a bug at the ship goes for. A cargo on board that no one took comes first, the nearest one. Else it gnaws
    // the place nearest to its own side: a module or hull point that can still take damage, else any of them, else
    // the middle of the deck. Every bug comes from its own side, so a group spreads over the ship.
    public sealed class BugTargetRule : IBugTargetRule {
        public BugTarget Choose(IReadOnlyList<IFieldAgentCargoTarget> cargoTargets, IReadOnlyList<ShipDamageCandidate> spots, Vector3 bugShipLocal) {
            if (TryFindNearestCargo(cargoTargets, bugShipLocal, out IFieldAgentCargoTarget cargo))
                return BugTarget.CreateCargo(cargo);

            if (TryFindNearestSpot(spots, bugShipLocal, true, out Vector3 spot) || TryFindNearestSpot(spots, bugShipLocal, false, out spot))
                return BugTarget.CreateGnaw(spot);

            return BugTarget.CreateGnaw(Vector3.zero);
        }

        private static bool TryFindNearestCargo(IReadOnlyList<IFieldAgentCargoTarget> cargoTargets, Vector3 bugShipLocal, out IFieldAgentCargoTarget nearest) {
            nearest = null;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < cargoTargets.Count; i++) {
                IFieldAgentCargoTarget candidate = cargoTargets[i];
                float distanceSqr = (candidate.ShipLocalPosition - bugShipLocal).sqrMagnitude;
                if (candidate.IsTaken || distanceSqr >= nearestSqr)
                    continue;

                nearest = candidate;
                nearestSqr = distanceSqr;
            }

            return nearest != null;
        }

        private static bool TryFindNearestSpot(IReadOnlyList<ShipDamageCandidate> spots, Vector3 bugShipLocal, bool isDamageableOnly, out Vector3 nearest) {
            nearest = Vector3.zero;
            bool isFound = false;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < spots.Count; i++) {
                ShipDamageCandidate candidate = spots[i];
                float distanceSqr = (candidate.Position - bugShipLocal).sqrMagnitude;
                bool isLeftOut = isDamageableOnly && candidate.IsDamageable == false;
                if (isLeftOut || distanceSqr >= nearestSqr)
                    continue;

                nearest = candidate.Position;
                nearestSqr = distanceSqr;
                isFound = true;
            }

            return isFound;
        }
    }
}
