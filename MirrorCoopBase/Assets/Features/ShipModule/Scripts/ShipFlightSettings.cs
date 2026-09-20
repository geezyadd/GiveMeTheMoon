using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(fileName = "ShipFlightConfig_Default", menuName = "Game/Ship Flight Config")]
    public sealed class ShipFlightSettings : ScriptableObject {
        [SerializeField] private float _dodgeRange = 4f;
        [SerializeField] private float _turnDegrees = 22f;
        [SerializeField] private float _bankDegrees = 22f;
        [SerializeField] private float _swayStiffness = 10f;
        [SerializeField] private float _swayDamping = 0.32f;

        public float DodgeRange => Mathf.Max(0f, _dodgeRange);
        public float TurnDegrees => _turnDegrees;
        public float BankDegrees => _bankDegrees;
        public float SwayStiffness => Mathf.Max(0.5f, _swayStiffness);
        public float SwayDamping => Mathf.Clamp(_swayDamping, 0.05f, 0.95f);
    }
}
