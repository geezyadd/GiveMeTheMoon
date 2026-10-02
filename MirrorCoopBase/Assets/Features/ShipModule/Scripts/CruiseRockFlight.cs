using UnityEngine;

namespace Features.ShipModule.Scripts {
    // One launch of a pooled rock. Every peer derives the rock's pose from it, so the rock needs no transform sync.
    // Start is expressed in the world frame of ShiftEpoch; later origin shifts are added by the reader.
    public struct CruiseRockFlight {
        public Vector3 Start;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public double LaunchTime;
        public double EndTime;
        public int ShiftEpoch;

        public bool IsVisibleAt(double time) =>
            time >= LaunchTime && time < EndTime;

        public Vector3 PositionAt(double time) =>
            Start + Velocity * (float)(time - LaunchTime);
    }
}
