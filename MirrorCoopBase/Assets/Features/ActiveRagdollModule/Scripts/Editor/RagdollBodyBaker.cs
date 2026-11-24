using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Features.ActiveRagdollModule.Editor {
    // Puts rigidbodies, capsules and joints on the bones a profile lists, and takes them off again.
    public static class RagdollBodyBaker {
        public static void Bake(RagdollBody body) {
            RagdollBodyProfile profile = body.Profile;
            Clear(body);

            List<RagdollLimb> limbs = new();
            List<RagdollLimbProfile> limbProfiles = new();
            foreach (RagdollLimbProfile limbProfile in profile.Limbs) {
                Transform bone = FindBone(body, limbProfile.BonePath);
                if (bone == null)
                    continue;

                limbs.Add(AddLimb(bone, limbProfile, profile));
                limbProfiles.Add(limbProfile);
            }

            for (int i = 0; i < limbs.Count; i++)
                ConnectLimb(body, limbs[i], limbProfiles[i], profile);

            Undo.RecordObject(body, "Bake ragdoll");
            body.SetLimbs(limbs);
            EditorUtility.SetDirty(body);
        }

        public static void Clear(RagdollBody body) {
            foreach (RagdollLimb limb in body.GetComponentsInChildren<RagdollLimb>(true)) {
                if (limb.Joint != null)
                    Undo.DestroyObjectImmediate(limb.Joint);
                // Only the capsule on the bone is the baker's; colliders the model already had stay.
                foreach (Collider limbCollider in limb.Colliders)
                    if (limbCollider is CapsuleCollider && limbCollider.gameObject == limb.gameObject)
                        Undo.DestroyObjectImmediate(limbCollider);
                Rigidbody rigidbody = limb.Rigidbody;
                Undo.DestroyObjectImmediate(limb);
                if (rigidbody != null)
                    Undo.DestroyObjectImmediate(rigidbody);
            }

            Undo.RecordObject(body, "Clear ragdoll");
            body.SetLimbs(new List<RagdollLimb>());
            EditorUtility.SetDirty(body);
        }

        private static RagdollLimb AddLimb(Transform bone, RagdollLimbProfile limbProfile, RagdollBodyProfile profile) {
            Rigidbody rigidbody = Undo.AddComponent<Rigidbody>(bone.gameObject);
            rigidbody.mass = limbProfile.Mass * profile.MassMultiplier;
            rigidbody.linearDamping = 0f;
            rigidbody.angularDamping = profile.AngularDamping;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

            return Undo.AddComponent<RagdollLimb>(bone.gameObject);
        }

        // Runs after every limb has a rigidbody, so a joint can find its parent limb.
        private static void ConnectLimb(RagdollBody body, RagdollLimb limb, RagdollLimbProfile limbProfile,
            RagdollBodyProfile profile) {
            RagdollLimb parent = FindParentLimb(body, limb.transform);
            CapsuleCollider capsule = LimbCapsuleShaper.AddCapsule(body, limb.transform, parent, limbProfile);
            ConfigurableJoint joint = parent == null ? null : AddJoint(limb, parent, profile);
            float damper = limbProfile.DriveSpring * profile.DamperToSpringRatio;

            Undo.RecordObject(limb, "Bake ragdoll");
            limb.Configure(limb.GetComponent<Rigidbody>(), joint, LimbColliders(limb, capsule), parent,
                limbProfile.DriveSpring, damper, limbProfile.Hovered, limbProfile.HeightReference);
            EditorUtility.SetDirty(limb);
        }

        // The capsule plus any solid collider the model already has on this bone or below it, short of the next limb:
        // they move with the limb, so they are switched and ignored together with it.
        private static Collider[] LimbColliders(RagdollLimb limb, CapsuleCollider capsule) {
            List<Collider> colliders = new() { capsule };
            foreach (Collider found in limb.GetComponentsInChildren<Collider>(true))
                if (found != capsule && found.isTrigger == false && found.GetComponentInParent<RagdollLimb>() == limb)
                    colliders.Add(found);
            return colliders.ToArray();
        }

        private static ConfigurableJoint AddJoint(RagdollLimb limb, RagdollLimb parent, RagdollBodyProfile profile) {
            ConfigurableJoint joint = Undo.AddComponent<ConfigurableJoint>(limb.gameObject);
            joint.connectedBody = parent.GetComponent<Rigidbody>();
            joint.axis = Vector3.right;
            joint.secondaryAxis = Vector3.up;
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;
            joint.lowAngularXLimit = new SoftJointLimit { limit = -profile.AngularLimit };
            joint.highAngularXLimit = new SoftJointLimit { limit = profile.AngularLimit };
            joint.angularYLimit = new SoftJointLimit { limit = profile.AngularLimit };
            joint.angularZLimit = new SoftJointLimit { limit = profile.AngularLimit };
            joint.rotationDriveMode = RotationDriveMode.XYAndZ;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.enablePreprocessing = true;
            joint.configuredInWorldSpace = false;
            joint.autoConfigureConnectedAnchor = true;
            joint.enableCollision = false;
            joint.breakForce = float.PositiveInfinity;
            joint.breakTorque = float.PositiveInfinity;
            return joint;
        }

        private static RagdollLimb FindParentLimb(RagdollBody body, Transform bone) {
            for (Transform parent = bone.parent; parent != null && parent != body.transform; parent = parent.parent) {
                RagdollLimb limb = parent.GetComponent<RagdollLimb>();
                if (limb != null)
                    return limb;
            }

            return null;
        }

        public static Transform FindBone(RagdollBody body, string path) {
            if (string.IsNullOrEmpty(path))
                return null;

            Transform bone = body.transform.Find(path);
            if (bone == null)
                Debug.LogWarning($"Ragdoll: bone '{path}' not found under {body.name}.", body);
            return bone;
        }
    }
}
