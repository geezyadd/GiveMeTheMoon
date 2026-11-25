using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Holds a standing body up instead of its legs: the hovered limbs are pushed up while the height limb is below
    // its animated height above the ground, and pulled down while it is above. The legs only follow the pose and
    // rest on the ground by their colliders. A crouch or any other pose works by itself, because the height
    // comes from the animation. After a landing the held height dips for a moment (LandingSag), deeper for a faster
    // landing, so the trunk sinks and comes back up; and when the legs no longer reach the floor (spread by a spin),
    // the held height drops until they do, so the body never floats over its legs.
    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(RagdollBody), typeof(RagdollGroundSensor))]
    public sealed class RagdollHover : MonoBehaviour {
        private readonly LandingSag _landingSag = new();

        private RagdollBody _body;
        private RagdollGroundSensor _ground;
        private bool _wasGrounded = true;
        private float _airVerticalSpeed;
        private float _legReachDrop;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
            _ground = GetComponent<RagdollGroundSensor>();
        }

        private void OnDisable() {
            _landingSag.Stop();
            _wasGrounded = true;
            _legReachDrop = 0f;
        }

        private void FixedUpdate() {
            RagdollStandSettings settings = _body.Profile.Stand;
            float sag = TrackLanding(settings);

            RagdollLimb heightLimb = _body.HeightLimb;
            if (_body.IsWeightless || _ground.IsGrounded == false || heightLimb == null || heightLimb.HasTarget == false) {
                _legReachDrop = 0f;
                return;
            }

            float height = heightLimb.Rigidbody.position.y - _ground.GroundPoint.y;
            float target = heightLimb.Target.Height - sag - TrackLegReach(settings);
            float push = Mathf.InverseLerp(target, target - settings.StandSmoothing, height);
            float pull = Mathf.InverseLerp(target, target + settings.StandSmoothing, height);
            Vector3 acceleration = Vector3.up * ((push - pull) * settings.StandForce * _body.ControlFactor);

            foreach (RagdollLimb limb in _body.Limbs)
                if (limb.IsHovered)
                    limb.AddAcceleration(acceleration);
        }

        // When the legs no longer reach the floor (spread wide, flung out by a spin), the held height drops by the gap
        // between the lowest leg point and the floor beyond the tolerance, eased over the smoothing time, so the body
        // sits down onto its legs instead of floating; it rises again as they come back under it. The legs are the
        // limbs off the trunk (not hovered); while one of them reaches the floor nothing drops.
        private float TrackLegReach(RagdollStandSettings settings) {
            float lowest = float.PositiveInfinity;
            foreach (RagdollLimb limb in _body.Limbs) {
                if (limb.IsHovered || limb.IsRoot)
                    continue;

                foreach (Collider limbCollider in limb.Colliders)
                    if (limbCollider != null && limbCollider.enabled)
                        lowest = Mathf.Min(lowest, limbCollider.bounds.min.y);
            }

            float gap = float.IsPositiveInfinity(lowest)
                ? 0f
                : Mathf.Max(lowest - _ground.GroundPoint.y - settings.LegReachTolerance, 0f);
            if (gap > 0f) {
                // The drop already applied lowers the legs too: the gap left goes on top of it.
                float share = settings.LegReachSmoothing > 0f
                    ? 1f - Mathf.Exp(-Time.fixedDeltaTime / settings.LegReachSmoothing)
                    : 1f;
                _legReachDrop = Mathf.Min(_legReachDrop + gap * share, settings.MaxLegReachDrop);
            }
            else {
                // A leg reaches the floor: the body stands back up slowly, and drops again if the legs lose the floor.
                _legReachDrop = Mathf.MoveTowards(_legReachDrop, 0f, settings.LegReachRiseSpeed * Time.fixedDeltaTime);
            }

            return _legReachDrop;
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
