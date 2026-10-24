using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    // Server: the flying ship's answer to a rock hit.
    internal interface IShipKnockback {
        public Vector3 Velocity { get; }
        public void ApplyKnockback(Vector3 velocity, float fadeSeconds, float maxAcceleration);
        public void SustainKnockback(Vector3 direction, float minimumSpeed);
    }
}
