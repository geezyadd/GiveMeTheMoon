using Features.ActiveRagdollModule;
using Features.CharacterMovableModule.Scripts.Body;
using Features.FloatingControllerModule;
using UnityEngine;

namespace Features.PlayerBodyModule {
    // A ragdoll as the body the player's movement drives: walking pushes every limb, a jump changes every limb's
    // velocity and pauses the hover, the ground comes from the ragdoll's own sensor.
    public sealed class RagdollMovableBody : IMovableBody {
        private readonly RagdollBody _body;
        private readonly RagdollGroundSensor _ground;

        public RagdollMovableBody(RagdollBody body, RagdollGroundSensor ground, ICarrierRider rider) {
            _body = body;
            _ground = ground;
            Rider = rider;
        }

        public Vector3 Position => _body.Root.Rigidbody.position;
        public Vector3 Velocity => _body.AverageVelocity;
        public float Mass => _body.TotalMass;
        public bool IsKinematic => _body.Root == null || _body.Root.Rigidbody.isKinematic;
        public bool SensesGround => true;
        public bool HasSupport => _ground.IsGrounded;
        public Vector3 SupportNormal => _ground.GroundNormal;
        public ICarrierRider Rider { get; }

        public void AddAcceleration(Vector3 acceleration) =>
            _body.AddAccelerationToAll(acceleration);

        public void AddVelocityChange(Vector3 velocityChange) =>
            _body.AddVelocityChangeToAll(velocityChange);

        public void PauseSupport() =>
            _ground.PauseStanding(_body.Profile.Stand.StandPauseAfterJump);

        public void MoveBy(Vector3 offset) =>
            _body.MoveAllBy(offset);

        public void SetVelocity(Vector3 velocity) =>
            _body.ShiftVelocities(velocity - _body.AverageVelocity);

        public void SetWeightless(bool isWeightless) =>
            _body.IsWeightless = isWeightless;
    }
}
