using UnityEngine;

namespace Features.ShipModule.Scripts.Turret {
    // How one turret model turns and how its aim travels over the network. Data only, referenced by the slot view.
    [CreateAssetMenu(menuName = "Game/Ship Turret Definition", fileName = "ShipTurretDefinition")]
    public sealed class ShipTurretDefinition : ScriptableObject {
        [Tooltip("Largest turn to each side of the mount's forward; 180 or more turns all the way round.")]
        [SerializeField, Range(0f, 180f)] private float _yawLimitDegrees = 180f;
        [Tooltip("Lowest barrel angle; negative is below the horizon.")]
        [SerializeField, Range(-90f, 0f)] private float _minPitchDegrees = -10f;
        [SerializeField, Range(0f, 90f)] private float _maxPitchDegrees = 75f;
        [Tooltip("Degrees of turn per unit of look input (mouse delta).")]
        [SerializeField, Min(0f)] private float _lookSensitivity = 0.12f;
        [Tooltip("Seconds other players' view of the barrel takes to catch up with the received aim.")]
        [SerializeField, Min(0f)] private float _remoteAimSmoothingSeconds = 0.08f;
        [Tooltip("The owner sends its aim at most this often; a trigger change goes out at once.")]
        [SerializeField, Min(0f)] private float _aimSendIntervalSeconds = 0.05f;

        public float YawLimitDegrees => _yawLimitDegrees;
        public float MinPitchDegrees => _minPitchDegrees;
        public float MaxPitchDegrees => _maxPitchDegrees;
        public float LookSensitivity => _lookSensitivity;
        public float RemoteAimSmoothingSeconds => _remoteAimSmoothingSeconds;
        public float AimSendIntervalSeconds => _aimSendIntervalSeconds;
    }
}
