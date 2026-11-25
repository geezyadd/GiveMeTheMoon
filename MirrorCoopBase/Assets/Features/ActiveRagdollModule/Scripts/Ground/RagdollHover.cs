using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Holds a standing body up instead of its legs: the hovered limbs are pushed up while the height limb is below
    // its animated height above the ground, and pulled down while it is above. The legs only follow the pose and
    // rest on the ground by their colliders. A crouch or any other pose works by itself, because the height
    // comes from the animation. After a landing the held height dips for a moment (LandingSag), deeper for a faster
    // landing, so the trunk sinks and comes back up.
    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(RagdollBody), typeof(RagdollGroundSensor))]
    public sealed class RagdollHover : MonoBehaviour {
        private readonly LandingSag _landingSag = new();

        private RagdollBody _body;
        private RagdollGroundSensor _ground;
        private bool _wasGrounded = true;
        private float _airVerticalSpeed;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
            _ground = GetComponent<RagdollGroundSensor>();
        }

        private void OnDisable() {
            _landingSag.Stop();
            _wasGrounded = true;
        }

        private void FixedUpdate() {
            RagdollStandSettings settings = _body.Profile.Stand;
            float sag = TrackLanding(settings);

            RagdollLimb heightLimb = _body.HeightLimb;
            if (_body.IsWeightless || _ground.IsGrounded == false || heightLimb == null || heightLimb.HasTarget == false)
                return;

            float height = heightLimb.Rigidbody.position.y - _ground.GroundPoint.y;
            float target = heightLimb.Target.Height - sag;
            float push = Mathf.InverseLerp(target, target - settings.StandSmoothing, height);
            float pull = Mathf.InverseLerp(target, target + settings.StandSmoothing, height);
            Vector3 acceleration = Vector3.up * ((push - pull) * settings.StandForce * _body.ControlFactor);

            foreach (RagdollLimb limb in _body.Limbs)
                if (limb.IsHovered)
                    limb.AddAcceleration(acceleration);
        }

        // Starts a sag on the step the body touches down, from the speed it fell at on the step before; returns this
        // step's depth.
        private float TrackLanding(RagdollStandSettings settings) {
            bool grounded = _ground.IsGrounded && _body.IsWeightless == false;
            if (grounded && _wasGrounded == false)
                _landingSag.Begin(-_airVerticalSpeed, settings);
            else if (grounded == false)
                _landingSag.Stop();

            _wasGrounded = grounded;
            _airVerticalSpeed = _body.AverageVelocity.y;
            return _landingSag.Step(Time.fixedDeltaTime);
        }
    }
}
