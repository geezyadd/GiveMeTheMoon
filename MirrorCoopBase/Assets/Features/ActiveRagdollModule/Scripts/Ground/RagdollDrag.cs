using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Takes a share of every limb's velocity each physics step. This sets the top speed (a pushing force evens out
    // with it), gives a quick viscous stop and keeps the hover from bouncing. Scaled by the control factor, so a limp
    // body keeps its velocity. The share is given per 1/60 s, so the feel stays the same at any physics rate.
    [DefaultExecutionOrder(-75)]
    [RequireComponent(typeof(RagdollBody), typeof(RagdollGroundSensor))]
    public sealed class RagdollDrag : MonoBehaviour {
        private const float ReferenceStepsPerSecond = 60f;

        private RagdollBody _body;
        private RagdollGroundSensor _ground;

        // An owner that sets the walking speed itself (a player's movement) turns these off: the drag then only
        // steadies the hover and the spin, and leaves walking and falling to the owner.
        public bool DampsHorizontal { get; set; } = true;
        public bool DampsInAir { get; set; } = true;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
            _ground = GetComponent<RagdollGroundSensor>();
        }

        private void FixedUpdate() {
            float keptPerStep = Mathf.Pow(_body.Profile.Stand.VelocityKeptPer60th,
                Time.fixedDeltaTime * ReferenceStepsPerSecond);
            float kept = Mathf.Lerp(1f, keptPerStep, _body.ControlFactor);
            bool dampsLinear = _ground.IsGrounded || DampsInAir;
            float horizontalKept = DampsHorizontal ? kept : 1f;
            Vector3 linearKept = new(horizontalKept, kept, horizontalKept);

            foreach (RagdollLimb limb in _body.Limbs) {
                Rigidbody rigidbody = limb.Rigidbody;
                if (rigidbody.isKinematic)
                    continue;

                if (dampsLinear)
                    rigidbody.linearVelocity = Vector3.Scale(rigidbody.linearVelocity, linearKept);
                rigidbody.angularVelocity *= kept;
            }
        }
    }
}
