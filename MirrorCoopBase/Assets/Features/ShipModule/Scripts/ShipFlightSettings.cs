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
        [SerializeField] private float _boardingEdgeTolerance = 0.5f;
        [SerializeField] private float _boardingMinHeight = -0.5f;
        [SerializeField] private float _boardingMaxHeight = 6f;
        [Tooltip("On: the deck edge stops riders. Off: walking or jumping past the deck edge drops the rider off the ship.")]
        [SerializeField] private bool _confineRidersToDeck;
        [Tooltip("After dropping off the deck in flight, the rider cannot land back on it for this long (s).")]
        [SerializeField] private float _reboardDelaySeconds = 0.5f;
        [Tooltip("In flight a rider who comes down on the deck is bound to it from this height above the deck surface under it (m).")]
        [SerializeField] private float _catchMinHeight = -0.5f;
        [Tooltip("In flight a rider who comes down on the deck is bound to it up to this height above the deck surface under it (m).")]
        [SerializeField] private float _catchMaxHeight = 1.5f;
        [Tooltip("In flight a rider rising faster than this (m/s) is not bound to the deck yet: it is still on its way up.")]
        [SerializeField] private float _catchMaxRiseSpeed = 0.15f;
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
        public float BoardingEdgeTolerance => _boardingEdgeTolerance;
        public float BoardingMinHeight => _boardingMinHeight;
        public float BoardingMaxHeight => _boardingMaxHeight;
        public bool ConfineRidersToDeck => _confineRidersToDeck;
        public float ReboardDelaySeconds => Mathf.Max(0f, _reboardDelaySeconds);
        public float CatchMinHeight => _catchMinHeight;
        public float CatchMaxHeight => _catchMaxHeight;
        public float CatchMaxRiseSpeed => _catchMaxRiseSpeed;
        public float HelmSteerSendSeconds => Mathf.Max(0f, _helmSteerSendSeconds);
    }
}
