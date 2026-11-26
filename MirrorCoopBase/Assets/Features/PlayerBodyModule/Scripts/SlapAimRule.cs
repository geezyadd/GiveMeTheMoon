using System.Collections.Generic;
using UnityEngine;

namespace Features.PlayerBodyModule {
    // The slap's aim, pure: which head the hand goes for and how strongly the arm reaches for it during the slap.
    public static class SlapAimRule {
        // The nearest head in front of the slapper: within maxDistance of its own head and at most maxAngle degrees
        // to either side of its facing (on the ground plane). -1 when there is none.
        public static int PickHead(Vector3 ownHead, float facingYaw, IReadOnlyList<Vector3> heads, float maxDistance,
            float maxAngle) {
            Vector3 forward = Quaternion.Euler(0f, facingYaw, 0f) * Vector3.forward;
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < heads.Count; i++) {
                Vector3 offset = heads[i] - ownHead;
                float distance = offset.magnitude;
                if (distance > maxDistance || distance >= bestDistance)
                    continue;

                Vector3 flat = new(offset.x, 0f, offset.z);
                if (flat.sqrMagnitude > 0.0001f && Vector3.Angle(forward, flat) > maxAngle)
                    continue;

                best = i;
                bestDistance = distance;
            }

            return best;
        }

        // The arm's reach weight, seconds since the slap started: 0 until startAt (the plain animation, its wind-up),
        // eases in to 1 by fullAt, holds 1 until releaseAt, eases out to 0 by endAt.
        public static float Weight(float sinceSlap, float startAt, float fullAt, float releaseAt, float endAt) {
            if (sinceSlap <= startAt || sinceSlap >= endAt)
                return 0f;
            if (sinceSlap < fullAt)
                return Mathf.SmoothStep(0f, 1f, (sinceSlap - startAt) / (fullAt - startAt));
            if (sinceSlap <= releaseAt)
                return 1f;

            float fadeSeconds = endAt - releaseAt;
            return fadeSeconds > 0f ? Mathf.SmoothStep(1f, 0f, (sinceSlap - releaseAt) / fadeSeconds) : 0f;
        }
    }
}
