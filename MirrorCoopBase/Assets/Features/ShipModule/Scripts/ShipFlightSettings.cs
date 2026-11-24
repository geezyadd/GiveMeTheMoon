using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(fileName = "ShipFlightConfig_Default", menuName = "Game/Ship Flight Config")]
    public sealed class ShipFlightSettings : ScriptableObject {
        [SerializeField] private ShipFlightMode _flightMode = ShipFlightMode.TravelInSpace;
        [Tooltip("The cruise speed changes by at most this much per second when the throttle moves or the cruise starts (m/s²).")]
        [SerializeField] private float _cruiseAcceleration = 8f;
        [SerializeField] private float _dodgeRange = 4f;
        [SerializeField] private float _turnRate = 80f;
        [SerializeField] private float _cruiseSpeed = 14f;
        [SerializeField] private float _bankDegrees = 22f;
        [SerializeField] private float _swayStiffness = 10f;
        [SerializeField] private float _swayDamping = 0.32f;
        [Tooltip("In flight an item let go over the deck up to this height above the deck surface under it falls onto the deck and flies with the ship (m).")]
        [SerializeField] private float _cargoCatchMaxHeight = 6f;
        [Tooltip("A pilot on a client sends its helm steer to the server at most this often (s).")]
        [SerializeField] private float _helmSteerSendSeconds = 0.03f;

        public ShipFlightMode FlightMode =>
            _flightMode == ShipFlightMode.None ? ShipFlightMode.TravelInSpace : _flightMode;

        public float CruiseAcceleration => Mathf.Max(0f, _cruiseAcceleration);
        public float DodgeRange => Mathf.Max(0f, _dodgeRange);
        public float TurnRate => Mathf.Max(10f, _turnRate);
        public float CruiseSpeed => Mathf.Max(0f, _cruiseSpeed);
        public float BankDegrees => _bankDegrees;
        public float SwayStiffness => Mathf.Max(0.5f, _swayStiffness);
        public float SwayDamping => Mathf.Clamp(_swayDamping, 0.05f, 0.95f);
        public float CargoCatchMaxHeight => _cargoCatchMaxHeight;
        public float HelmSteerSendSeconds => Mathf.Max(0f, _helmSteerSendSeconds);
    }
}
