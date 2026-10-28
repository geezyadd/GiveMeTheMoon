using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Behaviours {
    // Where a behaviour wants the agent: a point in the world or in ship space, how close counts as there, the share
    // of the top speed to go with, and whether reaching it lands the agent on the deck there.
    public readonly struct FieldAgentGoal {
        public Vector3 Point { get; }
        public bool IsShipSpace { get; }
        public float ArriveRadius { get; }
        public float SpeedShare { get; }
        public bool IsLandingOnArrival { get; }

        public FieldAgentGoal(Vector3 point, bool isShipSpace, float arriveRadius, float speedShare, bool isLandingOnArrival) {
            Point = point;
            IsShipSpace = isShipSpace;
            ArriveRadius = arriveRadius;
            SpeedShare = speedShare;
            IsLandingOnArrival = isLandingOnArrival;
        }
    }
}
