using Features.ActiveRagdollModule;
using UnityEngine;

namespace Features.PlayerBodyModule {
    // The ragdoll's components the player turns on and off together.
    public sealed class RagdollBodyParts {
        private readonly Behaviour[] _running;

        public RagdollBodyParts(RagdollBody body) {
            Sampler = body.GetComponent<AnimatedPoseSampler>();
            Ground = body.GetComponent<RagdollGroundSensor>();
            Gravity = body.GetComponent<RagdollGravity>();
            Drag = body.GetComponent<RagdollDrag>();
            Ride = body.GetComponent<RagdollCarrierRide>();
            Animator = Sampler.Animator != null ? Sampler.Animator : body.GetComponentInChildren<Animator>();
            _running = new Behaviour[] {
                body, Sampler, body.GetComponent<AnimatedPoseFollower>(), Ground, body.GetComponent<RagdollHover>(),
                Gravity, Drag, Ride, body.GetComponent<RagdollCarrierRideStepEnd>()
            };
        }

        public AnimatedPoseSampler Sampler { get; }
        public RagdollGroundSensor Ground { get; }
        public RagdollGravity Gravity { get; }
        public RagdollDrag Drag { get; }
        public RagdollCarrierRide Ride { get; }
        public Animator Animator { get; }

        public void SetRunning(bool running) {
            foreach (Behaviour part in _running)
                if (part != null)
                    part.enabled = running;
        }

        // The player's movement sets the walking speed, the jump and the fall; the ragdoll only stands and steadies.
        public void HandLocomotionToOwner() {
            Gravity.UsesPlainGravityInAir = true;
            Drag.DampsHorizontal = false;
            Drag.DampsInAir = false;
        }

        // Back to the model's rest pose and a still animator, as it stands inside the capsule.
        public void ResetPose() {
            if (Animator.isActiveAndEnabled) {
                Animator.Rebind();
                Animator.Update(0f);
            }

            Animator.enabled = false;
        }
    }
}
