using System;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Requests;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor {
    // A flight like the game plans one (ShipRoute): takeoff forward and up to the cruise start, then station B in a
    // cone ahead, farther every loop. The seed also turns the destination inside the cone.
    [Serializable]
    public sealed class FieldDebugScenario {
        // Keeps the direction roll apart from the generation sequence of the same seed.
        private const int DIRECTION_SEED_SALT = 0x5F3759DF;

        [SerializeField] private float _takeoffForward = 350f;
        [SerializeField] private float _takeoffHeight = 100f;
        // The game's flight length (ShipRunConfig): 120 s + 120 s per loop at the cruise speed of 14 m/s.
        [SerializeField] private float _baseDistance = 1680f;
        [SerializeField] private float _distancePerLoop = 1680f;
        [SerializeField] private float _destinationConeDegrees = 170f;

        public FieldDebugScenario() { }

        // A flight of another length, e.g. the loop tests' short flights.
        public FieldDebugScenario(float baseDistance, float distancePerLoop) {
            _baseDistance = baseDistance;
            _distancePerLoop = distancePerLoop;
        }

        public FieldRequest CreateRequest(IFieldRequestFactory requestFactory, int seed, int loopIndex) {
            Vector3 stationA = Vector3.zero;
            Vector3 cruiseStart = stationA + Vector3.forward * _takeoffForward + Vector3.up * _takeoffHeight;
            float halfCone = _destinationConeDegrees / 2f;
            float yaw = Mathf.Lerp(-halfCone, halfCone, new FieldRandom(seed ^ DIRECTION_SEED_SALT).Value());
            Vector3 destinationForward = Quaternion.AngleAxis(yaw, Vector3.up) * Vector3.forward;
            Vector3 stationB = cruiseStart + destinationForward * (_baseDistance + _distancePerLoop * loopIndex);
            stationB.y = stationA.y;
            return requestFactory.Create(stationA, stationB, cruiseStart, loopIndex, seed);
        }
    }
}
