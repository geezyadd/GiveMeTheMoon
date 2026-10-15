using UnityEngine;

namespace Features.GameCoreModule.Contracts {
    // One launched flight as the run planned it: from station A over the cruise start (where the takeoff ends) to
    // station B, on the given loop.
    public readonly struct FlightLaunch {
        public Vector3 StationA { get; }
        public Vector3 StationB { get; }
        public Vector3 CruiseStart { get; }
        public int LoopIndex { get; }

        public FlightLaunch(Vector3 stationA, Vector3 stationB, Vector3 cruiseStart, int loopIndex) {
            StationA = stationA;
            StationB = stationB;
            CruiseStart = cruiseStart;
            LoopIndex = loopIndex;
        }
    }
}
