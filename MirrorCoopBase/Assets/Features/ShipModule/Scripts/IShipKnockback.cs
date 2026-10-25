using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Server: the flying ship's answer to a rock hit.
    public interface IShipKnockback {
        public Vector3 Velocity { get; }
        public void ApplyKnockback(Vector3 velocity, float fadeSeconds, float maxAcceleration);
        public void SustainKnockback(Vector3 direction, float minimumSpeed);
    }
}
