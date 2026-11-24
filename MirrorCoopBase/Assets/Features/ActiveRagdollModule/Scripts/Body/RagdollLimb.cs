using UnityEngine;

namespace Features.ActiveRagdollModule {
    // One physical bone of a ragdoll body: its rigidbody, its joint to the parent limb and its colliders.
    // Set up by the editor baker from a RagdollBodyProfile.
    public sealed class RagdollLimb : MonoBehaviour {
        [SerializeField] private Rigidbody _rigidbody;
        [Tooltip("Empty on the root limb.")]
        [SerializeField] private ConfigurableJoint _joint;
        [SerializeField] private Collider[] _colliders = new Collider[0];
        [SerializeField] private RagdollLimb _parent;
        [SerializeField] private float _driveSpring;
        [SerializeField] private float _driveDamper;

        public Rigidbody Rigidbody => _rigidbody;
        public ConfigurableJoint Joint => _joint;
        public Collider[] Colliders => _colliders;
        public RagdollLimb Parent => _parent;
        public bool IsRoot => _joint == null;

        public void Configure(Rigidbody rigidbody, ConfigurableJoint joint, Collider[] colliders, RagdollLimb parent,
            float driveSpring, float driveDamper) {
            _rigidbody = rigidbody;
            _joint = joint;
            _colliders = colliders;
            _parent = parent;
            _driveSpring = driveSpring;
            _driveDamper = driveDamper;
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
    }
}
