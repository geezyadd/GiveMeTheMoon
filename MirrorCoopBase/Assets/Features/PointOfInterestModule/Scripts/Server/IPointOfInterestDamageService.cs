using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Server {
    public interface IPointOfInterestDamageService {
        // The point exists and its reward container still stands.
        public bool IsStanding(int pointId);
        // Server: a weapon hit on the reward container; at its hit points it is open.
        public void ServerApplyHit(int pointId, Vector3 hitPoint, float damage);
    }
}
