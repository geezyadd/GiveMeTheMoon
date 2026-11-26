using System;
using UnityEngine;

namespace Features.ActiveRagdollModule {
    // One physical bone of a ragdoll body: its rigidbody, its joint to the parent limb and its colliders.
    // Set up by the editor baker from a RagdollBodyProfile.
    public sealed class RagdollLimb : MonoBehaviour {
        [SerializeField] private Rigidbody _rigidbody;
        [Tooltip("Empty on the root limb.")]
        [SerializeField] private ConfigurableJoint _joint;
        [SerializeField] private Collider[] _colliders = Array.Empty<Collider>();
        [SerializeField] private RagdollLimb _parent;
        [SerializeField] private float _driveSpring;
        [SerializeField] private float _driveDamper;
        [SerializeField] private bool _hovered;
        [SerializeField] private bool _heightReference;

        private Quaternion _jointSpace = Quaternion.identity;
        private Vector3 _pendingForce;

        public Rigidbody Rigidbody => _rigidbody;
        public ConfigurableJoint Joint => _joint;
        public Collider[] Colliders => _colliders;
        public RagdollLimb Parent => _parent;
        public bool IsRoot => _joint == null;
        public bool IsHovered => _hovered;
        public bool IsHeightReference => _heightReference;
        // Rotation relative to the parent limb when the body was set up; the joint's rest pose.
        public Quaternion BindRotationInParent { get; private set; }
        public LimbPoseTarget Target { get; private set; }
        public bool HasTarget { get; private set; }

        public void Configure(Rigidbody rigidbody, ConfigurableJoint joint, Collider[] colliders, RagdollLimb parent,
            float driveSpring, float driveDamper, bool hovered, bool heightReference) {
            _rigidbody = rigidbody;
            _joint = joint;
            _colliders = colliders;
            _parent = parent;
            _driveSpring = driveSpring;
            _driveDamper = driveDamper;
            _hovered = hovered;
            _heightReference = heightReference;
        }

        public void CaptureBindPose() {
            BindRotationInParent = RotationInParent();
            if (_joint != null)
                _jointSpace = JointSpace(_joint);
        }

        // Called while the bones stand in the animated pose.
        public void CaptureTarget(Vector3 animatedRootPosition, float originHeight) {
            Target = new LimbPoseTarget(RotationInParent(), transform.forward, transform.up,
                IsRoot ? Vector3.zero : transform.position - animatedRootPosition, transform.position.y - originHeight);
            HasTarget = true;
        }

        // Puts the bone back where its rigidbody is, so the animated pose never reaches physics or the screen.
        public void ReturnToPhysics() {
            transform.SetPositionAndRotation(_rigidbody.position, _rigidbody.rotation);
        }

        // Scales the joint drive from limp (0) to the profile's stiffness (1).
        public void ApplyDriveScale(float scale) {
            if (_joint == null)
                return;

            JointDrive drive = new JointDrive {
                positionSpring = _driveSpring * scale,
                positionDamper = _driveDamper * scale,
                maximumForce = float.PositiveInfinity
            };
            _joint.angularXDrive = drive;
            _joint.angularYZDrive = drive;
        }

        // Points the joint drive at the target rotation. A joint's target is relative to its rest pose and
        // expressed in the joint's own axes, so the target is turned into that space.
        public void DriveJointToTarget() {
            if (_joint == null)
                return;

            Quaternion fromRest = Quaternion.Inverse(Target.RotationInParent) * BindRotationInParent;
            _joint.targetRotation = Quaternion.Inverse(_jointSpace) * fromRest * _jointSpace;
        }

        // Forces from everyone this step add up here and reach the rigidbody once, in ApplyPendingForce.
        public void AddForce(Vector3 force) {
            _pendingForce += force;
        }

        public void AddAcceleration(Vector3 acceleration) {
            _pendingForce += acceleration * _rigidbody.mass;
        }

        public void ApplyPendingForce() {
            if (_rigidbody.isKinematic == false && _pendingForce != Vector3.zero)
                _rigidbody.AddForce(_pendingForce, ForceMode.Force);
            _pendingForce = Vector3.zero;
        }

        // Every contact of this limb with anything, reported on each physics step it lasts.
        public event Action<RagdollLimb, Collision> Touching;

        private void OnCollisionEnter(Collision collision) {
            Touching?.Invoke(this, collision);
        }

        private void OnCollisionStay(Collision collision) {
            Touching?.Invoke(this, collision);
        }

        private Quaternion RotationInParent() =>
            _parent == null ? transform.rotation : Quaternion.Inverse(_parent.transform.rotation) * transform.rotation;

        private static Quaternion JointSpace(ConfigurableJoint joint) {
            Vector3 right = joint.axis;
            Vector3 forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
            Vector3 up = Vector3.Cross(forward, right).normalized;
            return Quaternion.LookRotation(forward, up);
        }
    }
}
