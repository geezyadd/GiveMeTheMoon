using UnityEditor;
using UnityEngine;

namespace Features.ActiveRagdollModule.Editor {
    // Fits a capsule on a bone: from the bone to its end bone, or along the parent limb's direction when the
    // profile gives no end bone (head, hands).
    public static class LimbCapsuleShaper {
        public static CapsuleCollider AddCapsule(RagdollBody body, Transform bone, RagdollLimb parent,
            RagdollLimbProfile limbProfile) {
            Vector3 localEnd = bone.InverseTransformPoint(EndPoint(body, bone, parent, limbProfile));
            int axis = LongestAxis(localEnd);
            float scale = Mathf.Abs(bone.lossyScale.x);
            float localRadius = scale > 0f ? limbProfile.Radius / scale : limbProfile.Radius;

            CapsuleCollider capsule = Undo.AddComponent<CapsuleCollider>(bone.gameObject);
            capsule.direction = axis;
            capsule.center = localEnd * 0.5f;
            capsule.radius = localRadius;
            capsule.height = Mathf.Max(localEnd.magnitude, localRadius * 2f);
            return capsule;
        }

        private static Vector3 EndPoint(RagdollBody body, Transform bone, RagdollLimb parent,
            RagdollLimbProfile limbProfile) {
            Transform endBone = RagdollBodyBaker.FindBone(body, limbProfile.EndBonePath);
            if (endBone != null)
                return endBone.position;

            Vector3 direction = parent != null ? (bone.position - parent.transform.position).normalized : bone.up;
            return bone.position + direction * limbProfile.FallbackLength;
        }

        private static int LongestAxis(Vector3 vector) {
            Vector3 size = new(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
            if (size.x >= size.y && size.x >= size.z)
                return 0;
            return size.y >= size.z ? 1 : 2;
        }
    }
}
