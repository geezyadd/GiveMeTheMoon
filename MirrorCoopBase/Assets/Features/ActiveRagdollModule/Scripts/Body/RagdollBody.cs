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

        public float TotalMass {
            get {
                float mass = 0f;
                foreach (RagdollLimb limb in _limbs)
                    mass += limb.Rigidbody.mass;
                return mass;
            }
        }

        // Mass-weighted velocity of the whole body.
        public Vector3 AverageVelocity {
            get {
                Vector3 momentum = Vector3.zero;
                float mass = 0f;
                foreach (RagdollLimb limb in _limbs) {
                    momentum += limb.Rigidbody.linearVelocity * limb.Rigidbody.mass;
                    mass += limb.Rigidbody.mass;
                }

                return mass > 0f ? momentum / mass : Vector3.zero;
            }
        }

        // Pushes every limb by the same acceleration this step (walking, extra gravity).
        public void AddAccelerationToAll(Vector3 acceleration) {
            foreach (RagdollLimb limb in _limbs)
                limb.AddAcceleration(acceleration);
        }

        // Changes every limb's velocity at once (a jump).
        public void AddVelocityChangeToAll(Vector3 velocityChange) {
            foreach (RagdollLimb limb in _limbs)
                if (limb.Rigidbody.isKinematic == false)
                    limb.Rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
        }

        // Shifts every limb at once, keeping the body's shape (a teleport or a network correction). The bone's transform
        // moves too: the pose sampler writes the transforms each step, and a moved transform is copied back onto its
        // rigidbody before the simulation, so moving the rigidbody alone would be undone. Parents go first (the list
        // order), so a parent's move never drags an already placed child.
        public void MoveAllBy(Vector3 offset) {
            foreach (RagdollLimb limb in _limbs) {
                Rigidbody rigidbody = limb.Rigidbody;
                if (rigidbody.isKinematic)
                    continue;

                Vector3 position = rigidbody.position + offset;
                limb.transform.SetPositionAndRotation(position, rigidbody.rotation);
                rigidbody.position = position;
            }
        }

        // Adds the same velocity to every limb right now, keeping how the limbs move against each other.
        public void ShiftVelocities(Vector3 delta) {
            foreach (RagdollLimb limb in _limbs)
                if (limb.Rigidbody.isKinematic == false)
                    limb.Rigidbody.linearVelocity += delta;
        }

        public void StopAllMotion() {
            foreach (RagdollLimb limb in _limbs) {
                if (limb.Rigidbody.isKinematic)
                    continue;

                limb.Rigidbody.linearVelocity = Vector3.zero;
                limb.Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        // Off while the limbs ride inside a moving parent as kinematic parts of a model: interpolation would keep
        // putting them back where their rigidbodies were and tear them away from the parent.
        public void SetInterpolation(bool interpolate) {
            RigidbodyInterpolation mode = interpolate ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            foreach (RagdollLimb limb in _limbs)
                limb.Rigidbody.interpolation = mode;
        }

        public void SetKinematic(bool isKinematic) {
            foreach (RagdollLimb limb in _limbs)
                limb.Rigidbody.isKinematic = isKinematic;
        }

        public void SetCollisions(bool enabled) {
            foreach (RagdollLimb limb in _limbs)
                foreach (Collider limbCollider in limb.Colliders)
                    limbCollider.enabled = enabled;

            // Turning a collider off forgets which pairs it ignored.
            if (enabled && _profile != null && _profile.IgnoreNearbyLimbCollisions)
                IgnoreNearbyLimbCollisions();
        }

        // Ignores collisions between every limb and the given colliders (the body that carries the ragdoll).
        public void IgnoreCollisionsWith(Collider other, bool ignore) {
            if (other == null)
                return;

            foreach (RagdollLimb limb in _limbs)
                foreach (Collider limbCollider in limb.Colliders)
                    Physics.IgnoreCollision(limbCollider, other, ignore);
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
