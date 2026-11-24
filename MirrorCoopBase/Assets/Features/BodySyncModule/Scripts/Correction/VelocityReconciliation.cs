using UnityEngine;

namespace Features.BodySyncModule.Correction {
    // Brings a copy's velocity to its owner's without overwriting it, axis by axis (in the platform's axes). When the
    // owner is slower on an axis, the copy's speed there is scaled down to the owner's, keeping its direction. When
    // the owner is faster, only the missing speed is added, in the owner's direction. A hit or a push the copy got
    // locally is therefore softened, not erased.
    public static class VelocityReconciliation {
        public static Vector3 Reconcile(Vector3 copyVelocity, Vector3 ownerVelocity) =>
            new(
                ReconcileAxis(copyVelocity.x, ownerVelocity.x),
                ReconcileAxis(copyVelocity.y, ownerVelocity.y),
                ReconcileAxis(copyVelocity.z, ownerVelocity.z));

        private static float ReconcileAxis(float copySpeed, float ownerSpeed) {
            float copySize = Mathf.Abs(copySpeed);
            float ownerSize = Mathf.Abs(ownerSpeed);
            if (ownerSize < copySize)
                return copySpeed * (ownerSize / copySize);

            return copySpeed + Mathf.Sign(ownerSpeed) * (ownerSize - copySize);
        }
    }
}
