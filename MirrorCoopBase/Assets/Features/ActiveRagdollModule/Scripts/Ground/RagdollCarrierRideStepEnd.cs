using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Runs after everything else in the physics step (execution order) and adds the carrier's move to the limbs'
    // velocities, once all forces of the step are in: see RagdollCarrierRide.
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(RagdollCarrierRide))]
    public sealed class RagdollCarrierRideStepEnd : MonoBehaviour {
        private RagdollCarrierRide _ride;

        private void Awake() {
            _ride = GetComponent<RagdollCarrierRide>();
        }

        private void FixedUpdate() {
            if (_ride.isActiveAndEnabled)
                _ride.AddCarry();
        }
    }
}
