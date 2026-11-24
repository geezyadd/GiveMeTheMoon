using Features.FloatingControllerModule;
using UnityEngine;

namespace Features.CharacterMovableModule.Scripts.Body {
    // The player's capsule: one rigidbody held over the ground by its floating controller.
    public sealed class CapsuleMovableBody : IMovableBody {
        private readonly Rigidbody _rigidbody;
        private readonly FloatingController _floatingController;

        public CapsuleMovableBody(Rigidbody rigidbody, FloatingController floatingController, ICarrierRider rider) {
            _rigidbody = rigidbody;
            _floatingController = floatingController;
            Rider = rider;
        }

        public Vector3 Position => _rigidbody.position;
        public Vector3 Velocity => _rigidbody.linearVelocity;
        public float Mass => _rigidbody.mass;
        public bool IsKinematic => _rigidbody.isKinematic;
        public bool SensesGround => _floatingController != null;
        public bool HasSupport => _floatingController != null && _floatingController.HasSupport;
        public Vector3 SupportNormal => _floatingController != null ? _floatingController.SupportNormal : Vector3.up;
        public ICarrierRider Rider { get; }

        public void AddAcceleration(Vector3 acceleration) =>
            _rigidbody.AddForce(acceleration, ForceMode.Acceleration);

        public void AddVelocityChange(Vector3 velocityChange) =>
            _rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);

        public void PauseSupport() {
            if (_floatingController != null)
                _floatingController.PauseSupport();
        }

        public void MoveBy(Vector3 offset) =>
            _rigidbody.position += offset;

        public void SetVelocity(Vector3 velocity) =>
            _rigidbody.linearVelocity = velocity;
    }
}
