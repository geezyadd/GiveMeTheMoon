using Features.GameCoreModule.Contracts;
using Features.PointOfInterestModule.Scripts.Network;
using Mirror;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Server {
    // Server: a world shift moves every point with the field, so a zone stays round its rocks' gap; the clients get the
    // moved points through the synced list.
    public sealed class PointOfInterestWorldShiftSystem : IWorldShiftFollower {
        private readonly IPointsOfInterest _pointsOfInterest;
        private readonly IPointOfInterestWriter _pointOfInterestWriter;

        public PointOfInterestWorldShiftSystem(IPointsOfInterest pointsOfInterest, IPointOfInterestWriter pointOfInterestWriter) {
            _pointsOfInterest = pointsOfInterest;
            _pointOfInterestWriter = pointOfInterestWriter;
        }

        public void FollowWorldShift(Vector3 delta) {
            if (!NetworkServer.active || !_pointOfInterestWriter.IsAvailable)
                return;

            for (int i = 0; i < _pointsOfInterest.Count; i++) {
                PointOfInterestEntry point = _pointsOfInterest.Get(i);
                point.Position += delta;
                _pointOfInterestWriter.ServerSet(i, point);
            }
        }
    }
}
