using Features.BodySyncModule.Correction;
using UnityEngine;

namespace Features.BodySyncModule.Configuration {
    // How players' bodies are synced: how often the owner sends its state, and how a remote copy is pulled toward it.
    // Start values; change them deliberately and write "was -> now".
    [CreateAssetMenu(fileName = nameof(BodySyncConfiguration) + "_Default", menuName = "Configurations/BodySync/" + nameof(BodySyncConfiguration))]
    public sealed class BodySyncConfiguration : ScriptableObject {
        [Header("Sending")]
        [Tooltip("State packets a second while the owner moves or anything in the packet changes.")]
        [SerializeField, Min(1f)] private float _sendRate = 30f;
        [Tooltip("State packets a second while the owner stands still and nothing changes: a sign of life.")]
        [SerializeField, Min(0.1f)] private float _idleSendRate = 5f;
        [Tooltip("Below this speed, m/s, the owner counts as standing still.")]
        [SerializeField, Min(0f)] private float _idleSpeed = 0.05f;

        [Header("Correction")]
        [Tooltip("Share of the horizontal error removed per physics step.")]
        [SerializeField, Range(0f, 1f)] private float _positionPull = 0.1f;
        [Tooltip("Below this vertical error, metres, the height is left to the copy's own physics.")]
        [SerializeField, Min(0f)] private float _verticalDeadZoneMin = 0.3f;
        [Tooltip("From this vertical error, metres, the vertical pull works at full strength.")]
        [SerializeField, Min(0f)] private float _verticalDeadZoneMax = 0.6f;
        [Tooltip("The largest vertical move per physics step, metres.")]
        [SerializeField, Min(0f)] private float _verticalPullPerStep = 0.1f;
        [Tooltip("How far ahead the target is moved along the packet's velocity at most, seconds.")]
        [SerializeField, Min(0f)] private float _maxExtrapolationSeconds = 0.15f;
        [Tooltip("Past this error, metres, the copy is moved to the target at once.")]
        [SerializeField, Min(0f)] private float _snapDistance = 3f;
        [Tooltip("How long owner and copy may disagree on standing or flying, seconds, before the vertical dead zone is dropped.")]
        [SerializeField, Min(0f)] private float _groundMismatchSeconds = 0.5f;

        public float SendInterval => 1f / _sendRate;
        public float IdleSendInterval => 1f / _idleSendRate;
        public float IdleSpeed => _idleSpeed;

        public BodyCorrectionSettings CorrectionSettings =>
            new() {
                PositionPull = _positionPull,
                VerticalDeadZoneMin = _verticalDeadZoneMin,
                VerticalDeadZoneMax = _verticalDeadZoneMax,
                VerticalPullPerStep = _verticalPullPerStep,
                MaxExtrapolationSeconds = _maxExtrapolationSeconds,
                SnapDistance = _snapDistance,
                GroundMismatchSeconds = _groundMismatchSeconds
            };
    }
}
