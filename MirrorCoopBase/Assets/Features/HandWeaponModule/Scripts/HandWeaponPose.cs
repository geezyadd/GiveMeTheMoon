using UnityEngine;

namespace Features.HandWeaponModule.Scripts {
    // Where a weapon in the hand sits, as a local pose under the arm point the held item follows: in its holder's
    // first-person view it sits at an offset in the view and turns with the look; in the hand it sits at an offset from
    // the arm point and points along the aim.
    public sealed class HandWeaponPose {
        // A synced aim comes from another peer and may be empty: the weapon then points along the arm.
        private const float MIN_AIM_SQUARED_LENGTH = 1e-6f;

        public Pose InView(Transform arm, Transform view, Vector3 viewOffset) {
            Quaternion toArm = Quaternion.Inverse(arm.rotation);
            Vector3 position = view.position + view.rotation * viewOffset;
            return new Pose(toArm * (position - arm.position), toArm * view.rotation);
        }

        public Pose InHand(Transform arm, Vector3 aimDirection, Vector3 handOffset) {
            Vector3 armAim = Quaternion.Inverse(arm.rotation) * aimDirection;
            Quaternion rotation = armAim.sqrMagnitude < MIN_AIM_SQUARED_LENGTH ? Quaternion.identity : Quaternion.LookRotation(armAim);
            return new Pose(handOffset, rotation);
        }
    }
}
