using System.Collections.Generic;
using UnityEngine;

namespace Features.ActiveRagdollModule {
    // The root of a ragdoll body: knows its limbs and does what concerns all of them at once.
    // It knows nothing about who owns the body (a player, a creature): they use it through this API.
    // Runs after everything that adds forces to the limbs this step, and hands them to physics in one go.
    [DefaultExecutionOrder(100)]
    public sealed class RagdollBody : MonoBehaviour {
        [SerializeField] private RagdollBodyProfile _profile;
        [SerializeField] private List<RagdollLimb> _limbs = new();

        public RagdollBodyProfile Profile => _profile;
        public IReadOnlyList<RagdollLimb> Limbs => _limbs;
        public RagdollLimb Root => _limbs.Count > 0 ? _limbs[0] : null;
        // The limb kept at its animated height above the ground (the head of a person); the root if none is marked.
        public RagdollLimb HeightLimb => _limbs.Find(limb => limb.IsHeightReference) ?? Root;
        // 0 = a limp ragdoll, 1 = fully follows the animation. Always 1 until falling and getting up are built.
        public float ControlFactor { get; private set; } = 1f;

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

            ApplyDriveScale(ControlFactor);
        }

        private void FixedUpdate() {
            foreach (RagdollLimb limb in _limbs)
                limb.ApplyPendingForce();
        }

        public void ApplyDriveScale(float scale) {
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
