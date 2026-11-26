using UnityEditor;
using UnityEngine;

namespace Features.ActiveRagdollModule.Editor {
    // Puts a limb profile's joint limits on its baked joint: the joint's primary axis is the bend axis (with an
    // asymmetric limit), its secondary axis the bone's direction (twist), the third the swing. The limits count from
    // the pose the joint starts in, the bind pose. PhysX counts the angle about the joint's axis against Unity's
    // rotation sense (checked 2026-11-26: a +5° Unity turn about +X met a -5° low limit), so the joint gets the
    // negated bend axis and the angles stay in Unity's sense.
    public static class RagdollJointLimitsBaker {
        public static void ApplyAll(RagdollBody body) {
            foreach (RagdollLimbProfile limbProfile in body.Profile.Limbs) {
                Transform bone = RagdollBodyBaker.FindBone(body, limbProfile.BonePath);
                RagdollLimb limb = bone != null ? bone.GetComponent<RagdollLimb>() : null;
                if (limb != null && limb.Joint != null)
                    Apply(body, limb, limbProfile);
            }
        }

        public static void Apply(RagdollBody body, RagdollLimb limb, RagdollLimbProfile limbProfile) {
            ConfigurableJoint joint = limb.Joint;
            RagdollJointLimits limits = limbProfile.Limits;
            float free = body.Profile.AngularLimit;
            Undo.RecordObject(joint, "Apply ragdoll joint limits");

            if (limits == null || limits.IsSet == false) {
                joint.axis = Vector3.right;
                joint.secondaryAxis = Vector3.up;
                SetLimits(joint, -free, free, free, free);
            }
            else {
                Vector3 bend = limb.transform.InverseTransformDirection(body.transform.TransformDirection(limits.BendAxis))
                    .normalized;
                Vector3 bone = Vector3.ProjectOnPlane(BoneDirection(body, limb, limbProfile), bend);
                if (bone.sqrMagnitude < 0.0001f)
                    bone = Vector3.ProjectOnPlane(Vector3.up, bend);

                joint.axis = -bend;
                joint.secondaryAxis = bone.normalized;
                SetLimits(joint, limits.MinBend, limits.MaxBend, limits.Twist, limits.Swing);
            }

            EditorUtility.SetDirty(joint);
        }

        private static void SetLimits(ConfigurableJoint joint, float low, float high, float twist, float swing) {
            joint.lowAngularXLimit = new SoftJointLimit { limit = low };
            joint.highAngularXLimit = new SoftJointLimit { limit = high };
            joint.angularYLimit = new SoftJointLimit { limit = twist };
            joint.angularZLimit = new SoftJointLimit { limit = swing };
        }

        // The bone's direction in its own space: toward its end bone, else on from its parent limb.
        private static Vector3 BoneDirection(RagdollBody body, RagdollLimb limb, RagdollLimbProfile limbProfile) {
            Transform end = string.IsNullOrEmpty(limbProfile.EndBonePath)
                ? null
                : RagdollBodyBaker.FindBone(body, limbProfile.EndBonePath);
            Vector3 world = end != null
                ? end.position - limb.transform.position
                : limb.Parent != null ? limb.transform.position - limb.Parent.transform.position : Vector3.up;
            return limb.transform.InverseTransformDirection(world);
        }
    }
}
