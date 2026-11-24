using UnityEngine;

namespace Features.PlayerReplicaModule.Correction {
    // Brings a copy's velocity to its owner's without overwriting it, axis by axis (in the platform's axes). When the
    // owner is slower on an axis, the copy's speed there is scaled down to the owner's, keeping its direction. When
    // the owner is faster, only the missing speed is added, in the owner's direction. A hit or a push the copy got
    // locally is therefore softened, not erased.
    // While the owner presses no walk key the copy is only ever slowed: a braking owner's packet speaks of a moment a
    // little before the copy's, so adding its speed would push a stopping copy on past the owner.
    public static class VelocityReconciliation {
        public static Vector3 Reconcile(Vector3 copyVelocity, Vector3 ownerVelocity, bool mayAddSpeed) =>
            new(
                ReconcileAxis(copyVelocity.x, ownerVelocity.x, mayAddSpeed),
                ReconcileAxis(copyVelocity.y, ownerVelocity.y, mayAddSpeed),
                ReconcileAxis(copyVelocity.z, ownerVelocity.z, mayAddSpeed));

        private static float ReconcileAxis(float copySpeed, float ownerSpeed, bool mayAddSpeed) {
            float copySize = Mathf.Abs(copySpeed);
            float ownerSize = Mathf.Abs(ownerSpeed);
            if (ownerSize < copySize)
                return copySpeed * (ownerSize / copySize);

            if (mayAddSpeed == false)
                return copySpeed;

            return copySpeed + Mathf.Sign(ownerSpeed) * (ownerSize - copySize);
        }
    }
}
