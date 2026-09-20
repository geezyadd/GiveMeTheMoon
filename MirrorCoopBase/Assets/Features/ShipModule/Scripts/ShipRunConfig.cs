using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(fileName = "ShipRunConfig_Default", menuName = "Game/Ship Run Config")]
    public sealed class ShipRunConfig : ScriptableObject {
        [SerializeField] private float _takeoffForward = 22f;
        [SerializeField] private float _takeoffHeight = 12f;
        [SerializeField] private float _takeoffSeconds = 9f;
        [SerializeField] private float _cruiseSeconds = 20f;
        [SerializeField] private float _landingSeconds = 9f;
        [SerializeField] private float _perLoopCruiseSeconds = 8f;
        [SerializeField] private float _thrustCruiseBonus = 1.5f;
        [SerializeField] private float _stationSpacing = 48f;
        [SerializeField] private float _wreckSettleSeconds = 0.6f;
        [SerializeField] private int _maxLoops;
        [SerializeField] private float _rockSpawnInterval = 1.4f;
        [SerializeField] private float _rockSpeed = 36f;
        [SerializeField] private float _rockSpawnAhead = 48f;
        [SerializeField] private float _rockLateral = 4f;

        public float TakeoffForward => Mathf.Max(0.2f, _takeoffForward);
        public float TakeoffHeight => Mathf.Max(0.2f, _takeoffHeight);
        public float TakeoffSeconds => Mathf.Max(0.2f, _takeoffSeconds);
        public float CruiseSeconds => Mathf.Max(1f, _cruiseSeconds);
        public float LandingSeconds => Mathf.Max(0.2f, _landingSeconds);
        public float PerLoopCruiseSeconds => Mathf.Max(0f, _perLoopCruiseSeconds);
        public float ThrustCruiseBonus => Mathf.Max(0f, _thrustCruiseBonus);
        public float StationSpacing => Mathf.Max(2f, _stationSpacing);
        public float WreckSettleSeconds => Mathf.Max(0.1f, _wreckSettleSeconds);
        public int MaxLoops => Mathf.Max(0, _maxLoops);
        public float RockSpawnInterval => Mathf.Max(0.2f, _rockSpawnInterval);
        public float RockSpeed => Mathf.Max(1f, _rockSpeed);
        public float RockSpawnAhead => Mathf.Max(4f, _rockSpawnAhead);
        public float RockLateral => Mathf.Max(0.5f, _rockLateral);
    }
}
