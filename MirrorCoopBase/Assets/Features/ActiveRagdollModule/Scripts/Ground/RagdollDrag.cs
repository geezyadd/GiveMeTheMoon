using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Takes a share of every limb's velocity each physics step. This sets the top speed (a pushing force evens out
    // with it), gives a quick viscous stop and keeps the hover from bouncing. Scaled by the control factor, so a limp
    // body keeps its velocity. The share is given per 1/60 s, so the feel stays the same at any physics rate.
    [DefaultExecutionOrder(-75)]
    [RequireComponent(typeof(RagdollBody))]
    public sealed class RagdollDrag : MonoBehaviour {
        private const float ReferenceStepsPerSecond = 60f;

        private RagdollBody _body;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
        }

        private void FixedUpdate() {
            float keptPerStep = Mathf.Pow(_body.Profile.Stand.VelocityKeptPer60th,
                Time.fixedDeltaTime * ReferenceStepsPerSecond);
            float kept = Mathf.Lerp(1f, keptPerStep, _body.ControlFactor);

            foreach (RagdollLimb limb in _body.Limbs) {
                Rigidbody rigidbody = limb.Rigidbody;
                if (rigidbody.isKinematic)
                    continue;

                rigidbody.linearVelocity *= kept;
                rigidbody.angularVelocity *= kept;
            }
        }
    }
}
