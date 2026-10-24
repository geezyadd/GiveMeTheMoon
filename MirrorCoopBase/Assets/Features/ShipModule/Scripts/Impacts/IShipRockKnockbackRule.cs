using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    public interface IShipRockKnockbackRule {
        // Horizontally away from the rock's centre; straight back when the ship is right above or below it.
        public Vector3 CalculateAwayDirection(Vector3 shipPosition, Vector3 rockCenter, Vector3 heading);
        public ShipKnockbackImpulse CalculateSmallPush(Vector3 awayDirection, Vector3 heading, Vector3 shipVelocity);
        public ShipKnockbackImpulse CalculateLargeKnockback(Vector3 awayDirection, Vector3 heading, Vector3 shipVelocity);
        // How far ahead of the hull, along its motion, a large rock counts as touched.
        public Vector3 CalculateLargeLookAhead(Vector3 shipVelocity);
    }
}
