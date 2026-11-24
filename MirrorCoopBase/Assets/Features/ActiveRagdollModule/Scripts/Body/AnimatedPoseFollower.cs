using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Pulls every limb toward the pose the sampler captured this step, in three ways: the joint drive turns it
    // relative to its parent, a torque turns it toward its animated world axes (on the root this keeps the body
    // upright), and a spring force pulls it toward the physical root plus its animated offset, so the limbs follow
    // the body wherever it really is. Everything is scaled by the body's control factor.
    [DefaultExecutionOrder(-90)]
    [RequireComponent(typeof(RagdollBody))]
    public sealed class AnimatedPoseFollower : MonoBehaviour {
        // Half of a limb's spring force goes back to its parent and half to the root, so the limbs cannot push
        // the whole body through space.
        private const float CounterForceShare = 0.5f;

        private RagdollBody _body;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
        }

        private void FixedUpdate() {
            RagdollLimb root = _body.Root;
            if (root == null || root.HasTarget == false)
                return;

            float force = _body.Profile.PoseForce * _body.ControlFactor;
            float torque = _body.Profile.PoseTorque * _body.ControlFactor;
            foreach (RagdollLimb limb in _body.Limbs) {
                limb.DriveJointToTarget();
                TurnTowardTarget(limb, torque);
                if (limb.IsRoot == false)
                    PullTowardTarget(limb, root, force);
            }
        }

        private static void TurnTowardTarget(RagdollLimb limb, float strength) {
            Rigidbody rigidbody = limb.Rigidbody;
            if (rigidbody.isKinematic)
                return;

            Transform bone = limb.transform;
            Vector3 error = AxisError(bone.forward, limb.Target.Forward) + AxisError(bone.up, limb.Target.Up);
            rigidbody.AddTorque(error * strength, ForceMode.Acceleration);
        }

        // Rotation axis from one direction to another, as long as the angle between them in degrees.
        private static Vector3 AxisError(Vector3 from, Vector3 to) =>
            Vector3.Cross(from, to).normalized * Vector3.Angle(from, to);

        private static void PullTowardTarget(RagdollLimb limb, RagdollLimb root, float strength) {
            Vector3 target = root.Rigidbody.position + limb.Target.OffsetFromRoot;
            Vector3 force = (target - limb.Rigidbody.position) * strength * limb.Rigidbody.mass;
            limb.AddForce(force);
            limb.Parent.AddForce(-CounterForceShare * force);
            root.AddForce(-CounterForceShare * force);
        }
    }
}
