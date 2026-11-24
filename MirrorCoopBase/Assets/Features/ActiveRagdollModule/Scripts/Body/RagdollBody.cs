using System.Collections.Generic;
using UnityEngine;

namespace Features.ActiveRagdollModule {
    // The root of a ragdoll body: knows its limbs and does what concerns all of them at once.
    // It knows nothing about who owns the body (a player, a creature): they use it through this API.
    public sealed class RagdollBody : MonoBehaviour {
        [SerializeField] private RagdollBodyProfile _profile;
        [SerializeField] private List<RagdollLimb> _limbs = new();

        [Tooltip("Temporary until the pose following step: joint drive strength, 0 = limp, 1 = holds the pose " +
                 "the body was baked in.")]
        [Range(0f, 1f)]
        [SerializeField] private float _driveScale;

        public RagdollBodyProfile Profile => _profile;
        public IReadOnlyList<RagdollLimb> Limbs => _limbs;
        public RagdollLimb Root => _limbs.Count > 0 ? _limbs[0] : null;

        public void SetLimbs(List<RagdollLimb> limbs) {
            _limbs = limbs;
        }

        private void Awake() {
            if (_profile == null) {
                Debug.LogError($"Ragdoll: {name} has no body profile.", this);
                return;
            }

            foreach (RagdollLimb limb in _limbs) {
                limb.Rigidbody.maxAngularVelocity = _profile.MaxAngularVelocity;
                limb.CaptureBindPose();
            }

            if (_profile.IgnoreNearbyLimbCollisions)
                IgnoreNearbyLimbCollisions();

            ApplyDriveScale(_driveScale);
        }

        private void OnValidate() {
            if (Application.isPlaying && _profile != null)
                ApplyDriveScale(_driveScale);
        }

        public void ApplyDriveScale(float scale) {
            _driveScale = scale;
            foreach (RagdollLimb limb in _limbs)
                limb.ApplyDriveScale(scale);
        }

        public void StopAllMotion() {
            foreach (RagdollLimb limb in _limbs) {
                if (limb.Rigidbody.isKinematic)
                    continue;

                limb.Rigidbody.linearVelocity = Vector3.zero;
                limb.Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        public void SetKinematic(bool isKinematic) {
            foreach (RagdollLimb limb in _limbs)
                limb.Rigidbody.isKinematic = isKinematic;
        }

        public void SetCollisions(bool enabled) {
            foreach (RagdollLimb limb in _limbs)
                foreach (Collider limbCollider in limb.Colliders)
                    limbCollider.enabled = enabled;
        }

        // A joint already stops its own pair from colliding; this also covers a limb and its grandparent
        // and limbs sharing a parent (both thighs, the chest and the arms).
        private void IgnoreNearbyLimbCollisions() {
            for (int i = 0; i < _limbs.Count; i++)
                for (int j = i + 1; j < _limbs.Count; j++)
                    if (AreNearby(_limbs[i], _limbs[j]))
                        IgnoreCollisions(_limbs[i], _limbs[j]);
        }

        private static bool AreNearby(RagdollLimb first, RagdollLimb second) {
            if (first.Parent == second || second.Parent == first)
                return true;
            if (first.Parent != null && first.Parent == second.Parent)
                return true;
            if (first.Parent != null && first.Parent.Parent == second)
                return true;
            return second.Parent != null && second.Parent.Parent == first;
        }

        private static void IgnoreCollisions(RagdollLimb first, RagdollLimb second) {
            foreach (Collider firstCollider in first.Colliders)
                foreach (Collider secondCollider in second.Colliders)
                    Physics.IgnoreCollision(firstCollider, secondCollider);
        }
    }
}
