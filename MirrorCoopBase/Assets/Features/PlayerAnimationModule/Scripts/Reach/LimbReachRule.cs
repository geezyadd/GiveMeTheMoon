using System.Collections.Generic;
using UnityEngine;

namespace Features.PlayerAnimationModule.Reach {
    // The pure part of LimbReach: which request a limb follows and how its shown weight and goal ease.
    public static class LimbReachRule {
        // The request of the highest priority among those for this limb that have a weight and a goal; the latest one on
        // a tie. Null: the limb plays the plain animation.
        public static LimbReachRequest PickWinner(IReadOnlyList<LimbReachRequest> requests, LimbId limb) {
            LimbReachRequest winner = null;
            for (int i = 0; i < requests.Count; i++) {
                LimbReachRequest request = requests[i];
                if (request.Limb != limb || request.Weight <= 0f || request.TryGetGoal(out _) == false)
                    continue;

                if (winner == null || request.Priority > winner.Priority
                    || (request.Priority == winner.Priority && request.Order > winner.Order))
                    winner = request;
            }

            return winner;
        }

        // The shown weight moves toward the wanted one, the whole way 0 to 1 in blendSeconds; 0 seconds: at once.
        public static float StepWeight(float shown, float wanted, float blendSeconds, float deltaTime) =>
            blendSeconds > 0f ? Mathf.MoveTowards(shown, wanted, deltaTime / blendSeconds) : wanted;

        // The shown goal follows the wanted one smoothly (a jittering head does not shake the arm); smoothingSeconds is
        // about the time it takes to cover two thirds of the way; 0 seconds: at once.
        public static Vector3 StepGoal(Vector3 shown, Vector3 wanted, float smoothingSeconds, float deltaTime) =>
            smoothingSeconds > 0f
                ? Vector3.Lerp(shown, wanted, 1f - Mathf.Exp(-deltaTime / smoothingSeconds))
                : wanted;
    }
}
