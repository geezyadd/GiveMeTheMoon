using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Gravity of a body that follows its animation: none while it stands (the hover alone holds the height), its own
    // gravity in the air that grows smoothly from nothing to full, and Unity's gravity only for a limp body.
    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(RagdollBody), typeof(RagdollGroundSensor))]
    public sealed class RagdollGravity : MonoBehaviour {
        private RagdollBody _body;
        private RagdollGroundSensor _ground;

        // In the air the body falls with plain world gravity instead of its own ramped one. For an owner that runs its
        // own jump and air rules (a player's movement), which are tuned for plain gravity.
        public bool UsesPlainGravityInAir { get; set; }

        private void Awake() {
            _body = GetComponent<RagdollBody>();
            _ground = GetComponent<RagdollGroundSensor>();
        }

        private void FixedUpdate() {
            RagdollStandSettings settings = _body.Profile.Stand;
            bool limp = _body.ControlFactor < settings.UnityGravityBelowControl;
            foreach (RagdollLimb limb in _body.Limbs)
                limb.Rigidbody.useGravity = limp && _body.IsWeightless == false;

            if (_ground.IsGrounded || limp || _body.IsWeightless)
                return;

            _body.AddAccelerationToAll(UsesPlainGravityInAir ? Physics.gravity : OwnAirGravity(settings));
        }

        private Vector3 OwnAirGravity(RagdollStandSettings settings) {
            float ramp = settings.AirGravityRamp.Evaluate(_ground.SecondsSinceGrounded * settings.AirGravityRampSpeed);
            return Vector3.up * (settings.MaxAirGravity * ramp * _body.ControlFactor);
        }
    }
}
