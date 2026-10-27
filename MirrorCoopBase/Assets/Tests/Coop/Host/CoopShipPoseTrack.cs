#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using UnityEngine;

namespace Tests.Coop.Host {
    // The host's ship poses over the last seconds, from the flight's destination. A client shows the ship a moment
    // behind the server (snapshot interpolation): its pose must lie on this track, not on the host's pose of the same
    // instant.
    public sealed class CoopShipPoseTrack {
        private const float KEPT_SECONDS = 5f;

        private readonly List<float> _times = new List<float>();
        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<float> _yaws = new List<float>();

        public void Record(ShipBase ship, IReadOnlyShipRunModel run) {
            float now = Time.realtimeSinceStartup;
            _times.Add(now);
            _positions.Add(ship.transform.position - run.TransitDestination);
            _yaws.Add(ship.transform.eulerAngles.y);
            while (_times.Count > 0 && now - _times[0] > KEPT_SECONDS) {
                _times.RemoveAt(0);
                _positions.RemoveAt(0);
                _yaws.RemoveAt(0);
            }
        }

        // The recorded pose nearest to the given one: how far from it and how much its heading differs (degrees).
        public bool TryFindNearest(Vector3 fromDestination, float yaw, out float distance, out float yawDifference) {
            distance = float.MaxValue;
            yawDifference = float.MaxValue;
            for (int i = 0; i < _positions.Count; i++) {
                float candidate = Vector3.Distance(_positions[i], fromDestination);
                if (candidate >= distance)
                    continue;

                distance = candidate;
                yawDifference = Mathf.Abs(Mathf.DeltaAngle(_yaws[i], yaw));
            }

            return _positions.Count > 0;
        }
    }
}
#endif
