using UnityEngine;

namespace Features.HandWeaponModule.Scripts {
    // Where a weapon in the hand sits, as a local pose under the arm point the held item follows: its grip (a point in
    // the weapon's own frame) in the palm, its barrel along the aim, upright.
    public sealed class HandWeaponPose {
        // A synced aim comes from another peer and may be empty: the weapon then points along the arm point.
        private const float MIN_AIM_SQUARED_LENGTH = 1e-6f;

        public Pose AtGrip(Transform arm, Vector3 gripPoint, Vector3 aimDirection, Vector3 gripOffset) {
            Quaternion rotation = aimDirection.sqrMagnitude < MIN_AIM_SQUARED_LENGTH
                ? arm.rotation
                : Quaternion.LookRotation(aimDirection, Vector3.up);
            Vector3 position = gripPoint - rotation * gripOffset;
            Quaternion toArm = Quaternion.Inverse(arm.rotation);
            return new Pose(toArm * (position - arm.position), toArm * rotation);
        }
    }
}
