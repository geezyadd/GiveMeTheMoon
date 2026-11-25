using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Holds a standing body up instead of its legs: the hovered limbs are pushed up while the height limb is below
    // its animated height above the ground, and pulled down while it is above. The legs only follow the pose and
    // rest on the ground by their colliders. A crouch or any other pose works by itself, because the height
    // comes from the animation.
    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(RagdollBody), typeof(RagdollGroundSensor))]
    public sealed class RagdollHover : MonoBehaviour {
        private RagdollBody _body;
        private RagdollGroundSensor _ground;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
            _ground = GetComponent<RagdollGroundSensor>();
        }

        private void FixedUpdate() {
            RagdollLimb heightLimb = _body.HeightLimb;
            if (_body.IsWeightless || _ground.IsGrounded == false || heightLimb == null || heightLimb.HasTarget == false)
                return;

            RagdollStandSettings settings = _body.Profile.Stand;
            float height = heightLimb.Rigidbody.position.y - _ground.GroundPoint.y;
            float target = heightLimb.Target.Height;
            float push = Mathf.InverseLerp(target, target - settings.StandSmoothing, height);
            float pull = Mathf.InverseLerp(target, target + settings.StandSmoothing, height);
            Vector3 acceleration = Vector3.up * ((push - pull) * settings.StandForce * _body.ControlFactor);

            foreach (RagdollLimb limb in _body.Limbs)
                if (limb.IsHovered)
                    limb.AddAcceleration(acceleration);
        }
    }
}
