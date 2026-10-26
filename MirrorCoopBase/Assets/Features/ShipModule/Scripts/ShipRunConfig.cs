using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(fileName = "ShipRunConfig_Default", menuName = "Game/Ship Run Config")]
    public sealed class ShipRunConfig : ScriptableObject {
        [SerializeField] private float _takeoffForward = 22f;
        [SerializeField] private float _takeoffHeight = 12f;
        [SerializeField] private float _takeoffSeconds = 9f;
        [Tooltip("Cruise seconds of loop 0 in a straight line from the hover point to the next station at flight speed 1.")]
        [SerializeField] private float _firstFlightSeconds = 120f;
        [Tooltip("Cruise seconds added per loop: seconds(loop) = first + loop * per loop.")]
        [SerializeField] private float _perLoopFlightSeconds = 120f;
        [Tooltip("The most cruise seconds of a flight; 0 = no cap.")]
        [SerializeField] private float _maxFlightSeconds;
        [SerializeField] private float _landingSeconds = 9f;
        [SerializeField] private float _stationSpacing = 48f;
        [Tooltip("Random destination yaw cone in front of launch heading. 170 keeps the pad ahead, never behind.")]
        [SerializeField] private float _destinationConeDegrees = 170f;
        [SerializeField] private float _wreckSettleSeconds = 0.6f;
        [SerializeField] private int _maxLoops;
        [SerializeField] private float _approachDistance = 70f;
        [Tooltip("When a landed ship is farther than this from the origin, the station shifts back toward zero.")]
        [SerializeField] private float _originRecenterDistance = 800f;

        public float TakeoffForward => Mathf.Max(0.2f, _takeoffForward);
        public float TakeoffHeight => Mathf.Max(0.2f, _takeoffHeight);
        public float TakeoffSeconds => Mathf.Max(0.2f, _takeoffSeconds);
        public float FirstFlightSeconds => _firstFlightSeconds;
        public float PerLoopFlightSeconds => _perLoopFlightSeconds;
        public float MaxFlightSeconds => _maxFlightSeconds;
        public float LandingSeconds => Mathf.Max(0.2f, _landingSeconds);
        public float StationSpacing => Mathf.Max(2f, _stationSpacing);
        public float DestinationConeDegrees => Mathf.Clamp(_destinationConeDegrees, 10f, 170f);
        public float WreckSettleSeconds => Mathf.Max(0.1f, _wreckSettleSeconds);
        public int MaxLoops => Mathf.Max(0, _maxLoops);
        public float ApproachDistance => Mathf.Max(8f, _approachDistance);
        public float OriginRecenterDistance => Mathf.Max(50f, _originRecenterDistance);
    }
}
