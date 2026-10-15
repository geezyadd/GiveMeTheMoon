using UnityEngine;

namespace Features.GameCoreModule.Contracts {
    public static class RigidbodyStopExtensions {
        // Only a dynamic body keeps a velocity: writing one to a kinematic body is ignored and logs a warning, which
        // costs a warning every frame on a body that is pinned every frame.
        public static void StopIfDynamic(this Rigidbody body) {
            if (body.isKinematic)
                return;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
