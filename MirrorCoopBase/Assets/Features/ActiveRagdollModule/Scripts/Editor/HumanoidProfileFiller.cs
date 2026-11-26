using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Features.ActiveRagdollModule.Editor {
    // Fills a body profile for a Humanoid avatar: 15 limbs, masses and stiffness heavier and stiffer near the root.
    // Bones between limbs (Chest, Neck, shoulders, fingers, toes) ride on their parent limb.
    public static class HumanoidProfileFiller {
        private readonly struct LimbDefault {
            public readonly HumanBodyBones[] Bone;
            public readonly HumanBodyBones[] End;
            public readonly float Mass;
            public readonly float Spring;
            public readonly float Radius;
            public readonly float Length;

            public LimbDefault(HumanBodyBones[] bone, HumanBodyBones[] end, float mass, float spring, float radius,
                float length = 0.15f) {
                Bone = bone;
                End = end;
                Mass = mass;
                Spring = spring;
                Radius = radius;
                Length = length;
            }
        }

        private static readonly HumanBodyBones[] None = new HumanBodyBones[0];

        // The first entry is the root limb.
        private static readonly LimbDefault[] Defaults = {
            new(Of(HumanBodyBones.Hips), Of(HumanBodyBones.Spine), 50f, 0f, 0.13f),
            new(Of(HumanBodyBones.Spine), Of(HumanBodyBones.UpperChest, HumanBodyBones.Chest), 35f, 3500f, 0.12f),
            new(Of(HumanBodyBones.UpperChest, HumanBodyBones.Chest), Of(HumanBodyBones.Neck, HumanBodyBones.Head),
                35f, 3500f, 0.14f),
            new(Of(HumanBodyBones.Head), None, 25f, 2500f, 0.11f, 0.22f),
            new(Of(HumanBodyBones.LeftUpperLeg), Of(HumanBodyBones.LeftLowerLeg), 25f, 2500f, 0.08f),
            new(Of(HumanBodyBones.LeftLowerLeg), Of(HumanBodyBones.LeftFoot), 15f, 1500f, 0.06f),
            new(Of(HumanBodyBones.LeftFoot), Of(HumanBodyBones.LeftToes), 10f, 1000f, 0.045f),
            new(Of(HumanBodyBones.RightUpperLeg), Of(HumanBodyBones.RightLowerLeg), 25f, 2500f, 0.08f),
            new(Of(HumanBodyBones.RightLowerLeg), Of(HumanBodyBones.RightFoot), 15f, 1500f, 0.06f),
            new(Of(HumanBodyBones.RightFoot), Of(HumanBodyBones.RightToes), 10f, 1000f, 0.045f),
            new(Of(HumanBodyBones.LeftUpperArm), Of(HumanBodyBones.LeftLowerArm), 10f, 1000f, 0.05f),
            new(Of(HumanBodyBones.LeftLowerArm), Of(HumanBodyBones.LeftHand), 10f, 1000f, 0.045f),
            new(Of(HumanBodyBones.LeftHand), Of(HumanBodyBones.LeftMiddleProximal), 10f, 1000f, 0.04f, 0.1f),
            new(Of(HumanBodyBones.RightUpperArm), Of(HumanBodyBones.RightLowerArm), 10f, 1000f, 0.05f),
            new(Of(HumanBodyBones.RightLowerArm), Of(HumanBodyBones.RightHand), 10f, 1000f, 0.045f),
            new(Of(HumanBodyBones.RightHand), Of(HumanBodyBones.RightMiddleProximal), 10f, 1000f, 0.04f, 0.1f),
        };

        // Anatomical joint limits for a person facing +Z in a T-pose (min bend, max bend, twist, swing; degrees).
        // Trunk, head and legs bend about +X: a positive bend tips the trunk and head forward and swings a leg bone's
        // lower end back (hip extension, knee flexion, toes down). Arms bend about +Y on the left and -Y on the right:
        // a positive bend swings the arm forward (elbow flexion). Hands bend about +Z (wrist up / down).
        private static readonly Vector3 Right = Vector3.right;
        private static readonly Vector3 LeftArm = Vector3.up;
        private static readonly Vector3 RightArm = Vector3.down;
        private static readonly Dictionary<HumanBodyBones, RagdollJointLimits> Limits = new() {
            [HumanBodyBones.Spine] = new(Right, -20f, 30f, 15f, 15f),
            [HumanBodyBones.UpperChest] = new(Right, -15f, 25f, 15f, 10f),
            [HumanBodyBones.Chest] = new(Right, -15f, 25f, 15f, 10f),
            [HumanBodyBones.Head] = new(Right, -40f, 50f, 50f, 25f),
            [HumanBodyBones.LeftUpperLeg] = new(Right, -100f, 25f, 25f, 40f),
            [HumanBodyBones.RightUpperLeg] = new(Right, -100f, 25f, 25f, 40f),
            [HumanBodyBones.LeftLowerLeg] = new(Right, -2f, 140f, 5f, 5f),
            [HumanBodyBones.RightLowerLeg] = new(Right, -2f, 140f, 5f, 5f),
            [HumanBodyBones.LeftFoot] = new(Right, -30f, 45f, 10f, 20f),
            [HumanBodyBones.RightFoot] = new(Right, -30f, 45f, 10f, 20f),
            [HumanBodyBones.LeftUpperArm] = new(LeftArm, -60f, 120f, 80f, 85f),
            [HumanBodyBones.RightUpperArm] = new(RightArm, -60f, 120f, 80f, 85f),
            [HumanBodyBones.LeftLowerArm] = new(LeftArm, -2f, 145f, 70f, 5f),
            [HumanBodyBones.RightLowerArm] = new(RightArm, -2f, 145f, 70f, 5f),
            [HumanBodyBones.LeftHand] = new(Vector3.forward, -70f, 70f, 20f, 30f),
            [HumanBodyBones.RightHand] = new(Vector3.forward, -70f, 70f, 20f, 30f),
        };

        public static bool CanFill(RagdollBody body) {
            Animator animator = body.GetComponentInChildren<Animator>();
            return body.Profile != null && animator != null && animator.isHuman;
        }

        public static void Fill(RagdollBody body) {
            Animator animator = body.GetComponentInChildren<Animator>();
            RagdollBodyProfile profile = body.Profile;
            Undo.RecordObject(profile, "Fill humanoid ragdoll profile");
            profile.Limbs.Clear();
            float sizeScale = SizeScale(animator);

            foreach (LimbDefault limbDefault in Defaults) {
                Transform bone = FirstFound(animator, limbDefault.Bone);
                if (bone == null)
                    continue;

                Transform end = FirstFound(animator, limbDefault.End);
                profile.Limbs.Add(new RagdollLimbProfile {
                    BonePath = AnimationUtility.CalculateTransformPath(bone, body.transform),
                    EndBonePath = end == null ? string.Empty : AnimationUtility.CalculateTransformPath(end, body.transform),
                    Mass = limbDefault.Mass,
                    DriveSpring = limbDefault.Spring,
                    Radius = limbDefault.Radius * sizeScale,
                    FallbackLength = limbDefault.Length * sizeScale,
                    Hovered = IsTrunk(limbDefault.Bone[0]),
                    HeightReference = limbDefault.Bone[0] == HumanBodyBones.Head,
                    Limits = LimitsFor(animator, bone)
                });
            }

            EditorUtility.SetDirty(profile);
        }

        // Sets only the joint limits of the profile's limbs that are Humanoid bones, leaving every other value as tuned.
        public static void FillLimits(RagdollBody body) {
            Animator animator = body.GetComponentInChildren<Animator>();
            RagdollBodyProfile profile = body.Profile;
            Undo.RecordObject(profile, "Fill ragdoll joint limits");
            foreach (RagdollLimbProfile limb in profile.Limbs) {
                Transform bone = RagdollBodyBaker.FindBone(body, limb.BonePath);
                if (bone != null)
                    limb.Limits = LimitsFor(animator, bone);
            }

            EditorUtility.SetDirty(profile);
        }

        private static RagdollJointLimits LimitsFor(Animator animator, Transform bone) {
            foreach (KeyValuePair<HumanBodyBones, RagdollJointLimits> entry in Limits) {
                if (animator.GetBoneTransform(entry.Key) != bone)
                    continue;

                RagdollJointLimits limits = entry.Value;
                return new RagdollJointLimits(limits.BendAxis, limits.MinBend, limits.MaxBend, limits.Twist, limits.Swing);
            }

            return new RagdollJointLimits();
        }

        // The hover holds the trunk; the legs only follow the pose.
        private static bool IsTrunk(HumanBodyBones bone) =>
            bone is HumanBodyBones.Hips or HumanBodyBones.Spine or HumanBodyBones.UpperChest or HumanBodyBones.Head;

        // The default radii and lengths are for a person about 1.8 m tall (hips to head about 0.75 m).
        private static float SizeScale(Animator animator) {
            const float ReferenceHipsToHead = 0.75f;
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (hips == null || head == null)
                return 1f;

            return Vector3.Distance(hips.position, head.position) / ReferenceHipsToHead;
        }

        private static Transform FirstFound(Animator animator, HumanBodyBones[] bones) {
            foreach (HumanBodyBones bone in bones) {
                Transform found = animator.GetBoneTransform(bone);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static HumanBodyBones[] Of(params HumanBodyBones[] bones) => bones;
    }
}
