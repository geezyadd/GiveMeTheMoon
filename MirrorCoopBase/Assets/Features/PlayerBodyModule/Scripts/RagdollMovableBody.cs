using Features.ActiveRagdollModule;
using Features.CharacterMovableModule.Scripts.Body;
using UnityEngine;

namespace Features.PlayerBodyModule {
    // A ragdoll as the body the player's movement drives: walking pushes every limb, a jump changes every limb's
    // velocity and pauses the hover, the ground comes from the ragdoll's own sensor.
    public sealed class RagdollMovableBody : IMovableBody {
        private readonly RagdollBody _body;
        private readonly RagdollGroundSensor _ground;

        public RagdollMovableBody(RagdollBody body, RagdollGroundSensor ground) {
            _body = body;
            _ground = ground;
        }

        public Vector3 Velocity => _body.AverageVelocity;
        public float Mass => _body.TotalMass;
        public bool IsKinematic => _body.Root == null || _body.Root.Rigidbody.isKinematic;
        public bool SensesGround => true;
        public bool HasSupport => _ground.IsGrounded;
        public Vector3 SupportNormal => _ground.GroundNormal;

        public void AddAcceleration(Vector3 acceleration) =>
            _body.AddAccelerationToAll(acceleration);

        public void AddVelocityChange(Vector3 velocityChange) =>
            _body.AddVelocityChangeToAll(velocityChange);

        public void PauseSupport() =>
            _ground.PauseStanding(_body.Profile.Stand.StandPauseAfterJump);
    }
}
