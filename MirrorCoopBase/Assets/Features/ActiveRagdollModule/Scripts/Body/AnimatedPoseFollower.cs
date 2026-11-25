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

        // A target turning more than this in one step jumped rather than turned.
        private const float MaxTargetTurnPerStepDegrees = 45f;

        private RagdollBody _body;
        private Quaternion _lastRootTarget;
        private bool _hasLastRootTarget;

        private void OnDisable() =>
            _hasLastRootTarget = false;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
        }

        private void FixedUpdate() {
            RagdollLimb root = _body.Root;
            if (root == null || root.HasTarget == false)
                return;

            float force = _body.Profile.PoseForce * _body.ControlFactor;
            float damping = _body.Profile.PoseDamping * _body.ControlFactor;
            float torque = _body.Profile.PoseTorque * _body.ControlFactor;
            foreach (RagdollLimb limb in _body.Limbs) {
                limb.DriveJointToTarget();
                TurnTowardTarget(limb, torque);
                if (limb.IsRoot == false)
                    PullTowardTarget(limb, root, force, damping);
            }

            DampRootSwing(root, _body.Profile.RootSwingDamping * _body.ControlFactor);
        }

        // Slows the hips' spin against the spin of their animated target (the pose turning toward the look, a drift's
        // head-first tilt), so the hips stop swinging like a pendulum sooner while every wanted turn keeps its speed.
        private void DampRootSwing(RagdollLimb root, float damping) {
            Quaternion target = Quaternion.LookRotation(root.Target.Forward, root.Target.Up);
            Vector3 targetSpin = TargetSpin(target);
            _lastRootTarget = target;
            _hasLastRootTarget = true;

            Rigidbody rigidbody = root.Rigidbody;
            if (damping <= 0f || rigidbody.isKinematic)
                return;

            rigidbody.AddTorque(-(rigidbody.angularVelocity - targetSpin) * damping, ForceMode.Acceleration);
        }

        // How fast the target turned since the last step, as an angular velocity (radians per second).
        private Vector3 TargetSpin(Quaternion target) {
            if (_hasLastRootTarget == false)
                return Vector3.zero;

            (target * Quaternion.Inverse(_lastRootTarget)).ToAngleAxis(out float degrees, out Vector3 axis);
            if (degrees > 180f)
                degrees -= 360f;
            // A target that jumped (a snap of the look, the first step after waking) is no spin to follow.
            if (float.IsInfinity(axis.x) || float.IsNaN(axis.x) || Mathf.Abs(degrees) < 0.0001f
                || Mathf.Abs(degrees) > MaxTargetTurnPerStepDegrees)
                return Vector3.zero;

            return axis * (degrees * Mathf.Deg2Rad / Time.fixedDeltaTime);
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

        // The spring, damped by the limb's speed relative to the root: the counter-force goes back the same way, so the
        // damping only calms the limbs against each other and never slows the whole body (walking, falling, drifting).
        private static void PullTowardTarget(RagdollLimb limb, RagdollLimb root, float strength, float damping) {
            Vector3 target = root.Rigidbody.position + limb.Target.OffsetFromRoot;
            Vector3 relativeVelocity = limb.Rigidbody.linearVelocity - root.Rigidbody.linearVelocity;
            Vector3 acceleration = (target - limb.Rigidbody.position) * strength - relativeVelocity * damping;
            Vector3 force = acceleration * limb.Rigidbody.mass;
            limb.AddForce(force);
            limb.Parent.AddForce(-CounterForceShare * force);
            root.AddForce(-CounterForceShare * force);
        }
    }
}
