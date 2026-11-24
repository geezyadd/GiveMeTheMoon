using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Gravity of a body that follows its animation: none while it stands (the hover alone holds the height), its own
    // gravity in the air that grows smoothly from nothing to full, and Unity's gravity only for a limp body.
    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(RagdollBody), typeof(RagdollGroundSensor))]
    public sealed class RagdollGravity : MonoBehaviour {
        private RagdollBody _body;
        private RagdollGroundSensor _ground;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
            _ground = GetComponent<RagdollGroundSensor>();
        }

        private void FixedUpdate() {
            RagdollStandSettings settings = _body.Profile.Stand;
            bool limp = _body.ControlFactor < settings.UnityGravityBelowControl;
            foreach (RagdollLimb limb in _body.Limbs)
                limb.Rigidbody.useGravity = limp;

            if (_ground.IsGrounded)
                return;

            float ramp = settings.AirGravityRamp.Evaluate(_ground.SecondsSinceGrounded * settings.AirGravityRampSpeed);
            Vector3 acceleration = Vector3.up * (settings.MaxAirGravity * ramp * _body.ControlFactor);
            foreach (RagdollLimb limb in _body.Limbs)
                limb.AddAcceleration(acceleration);
        }
    }
}
