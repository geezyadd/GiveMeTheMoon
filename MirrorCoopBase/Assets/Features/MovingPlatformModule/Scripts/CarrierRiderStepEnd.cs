using UnityEngine;

namespace Features.MovingPlatformModule {
    // Runs after everything else in the physics step (execution order) and adds the carrier's move to the rider's
    // velocity, once all forces of the step are in: see CarrierRider.
    [DefaultExecutionOrder(1000)]
    public sealed class CarrierRiderStepEnd : MonoBehaviour {
        [SerializeField] private CarrierRider _rider;

        private void FixedUpdate() =>
            _rider.AddCarry();
    }
}
