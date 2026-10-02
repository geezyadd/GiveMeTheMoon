using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(fileName = "ShipFlightConfig_Default", menuName = "Game/Ship Flight Config")]
    public sealed class ShipFlightSettings : ScriptableObject {
        [SerializeField] private ShipFlightMode _flightMode = ShipFlightMode.TravelInSpace;
        [SerializeField] private float _travelAccelSeconds = 1.5f;
        [SerializeField] private float _dodgeRange = 4f;
        [SerializeField] private float _turnDegrees = 22f;
        [SerializeField] private float _turnRate = 80f;
        [SerializeField] private float _cruiseSpeed = 14f;
        [SerializeField] private float _bankDegrees = 22f;
        [SerializeField] private float _swayStiffness = 10f;
        [SerializeField] private float _swayDamping = 0.32f;
        [SerializeField] private float _boardingEdgeTolerance = 0.5f;
        [SerializeField] private float _boardingMinHeight = -0.5f;
        [SerializeField] private float _boardingMaxHeight = 6f;
        [SerializeField] private float _standUpSpeed = 6f;

        public ShipFlightMode FlightMode =>
            _flightMode == ShipFlightMode.None ? ShipFlightMode.TravelInSpace : _flightMode;

        public float TravelAccelSeconds => Mathf.Max(0f, _travelAccelSeconds);
        public float DodgeRange => Mathf.Max(0f, _dodgeRange);
        public float TurnDegrees => _turnDegrees;
        public float TurnRate => Mathf.Max(10f, _turnRate);
        public float CruiseSpeed => Mathf.Max(0f, _cruiseSpeed);
        public float BankDegrees => _bankDegrees;
        public float SwayStiffness => Mathf.Max(0.5f, _swayStiffness);
        public float SwayDamping => Mathf.Clamp(_swayDamping, 0.05f, 0.95f);
        public float BoardingEdgeTolerance => _boardingEdgeTolerance;
        public float BoardingMinHeight => _boardingMinHeight;
        public float BoardingMaxHeight => _boardingMaxHeight;
        public float StandUpSpeed => _standUpSpeed;
    }
}
