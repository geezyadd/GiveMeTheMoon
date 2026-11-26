using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Turns the body into a limp ragdoll for a while: its control factor drops to 0 at once (no pull toward the pose,
    // damped and narrowed joints), stays there for the knockdown's time, then grows back to 1 over the profile's recover time, so the pose forces stand the body up
    // again by themselves. A new knockdown while one runs keeps the longer limp time. Runs first of the ragdoll
    // components, so every force of the step reads this step's control.
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(RagdollBody))]
    public sealed class RagdollKnockdown : MonoBehaviour {
        private RagdollBody _body;
        private float _limpSecondsLeft;
        private bool _jointsChanged;

        // Limp or still getting the control back.
        public bool IsKnockedDown => _limpSecondsLeft > 0f || _body.ControlFactor < 1f;

        private void Awake() =>
            _body = GetComponent<RagdollBody>();

        private void OnDisable() {
            _limpSecondsLeft = 0f;
            if (_body == null)
                return;

            _body.SetControlFactor(1f);
            if (_jointsChanged)
                _body.ApplyLimpJoints(1f, 1f, 1f);
            _jointsChanged = false;
        }

        public void KnockDown(float limpSeconds) {
            if (limpSeconds <= 0f)
                return;

            _limpSecondsLeft = Mathf.Max(_limpSecondsLeft, limpSeconds);
            _body.SetControlFactor(0f);
        }

        private void FixedUpdate() {
            if (_limpSecondsLeft > 0f)
                _limpSecondsLeft -= Time.fixedDeltaTime;
            else if (_body.ControlFactor < 1f)
                Recover();

            if (_body.ControlFactor < 1f || _jointsChanged)
                ApplyJoints();
        }

        // While down nothing pulls the limbs toward the animation (the spring grows back only with the control), the
        // joints resist turning with the profile's damper share and their limits are narrowed; all of it eases back to
        // normal as the body gets up.
        private void ApplyJoints() {
            float control = _body.ControlFactor;
            RagdollBodyProfile profile = _body.Profile;
            float damper = Mathf.Max(control, profile.KnockdownJointDamperShare);
            float limits = Mathf.Lerp(profile.KnockdownJointLimitShare, 1f, control);
            _body.ApplyLimpJoints(control, damper, limits);
            _jointsChanged = control < 1f;
        }

        private void Recover() {
            float recoverSeconds = _body.Profile.KnockdownRecoverSeconds;
            float step = recoverSeconds > 0f ? Time.fixedDeltaTime / recoverSeconds : 1f;
            _body.SetControlFactor(_body.ControlFactor + step);
        }
    }
}
