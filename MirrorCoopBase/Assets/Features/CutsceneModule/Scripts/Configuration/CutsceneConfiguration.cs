using UnityEngine;

namespace Features.CutsceneModule.Scripts.Configuration {
    // The camera feel of every cutscene and the debug window's test cutscene.
    [CreateAssetMenu(fileName = nameof(CutsceneConfiguration), menuName = "Configurations/Cutscene/" + nameof(CutsceneConfiguration))]
    public sealed class CutsceneConfiguration : ScriptableObject {
        [Header("Timing")]
        [Tooltip("The server starts a cutscene this far ahead of now, so every client gets it before its first frame.")]
        [SerializeField, Min(0f)] private float _startLeadSeconds = 0.15f;
        [Tooltip("The blend from the cutscene camera back to the player's camera after the last shot.")]
        [SerializeField, Min(0f)] private float _returnBlendSeconds = 0.8f;

        [Header("Camera")]
        [Tooltip("How long the camera takes to turn onto a shot's focus at the start of the shot.")]
        [SerializeField, Min(0.01f)] private float _turnSeconds = 0.7f;
        [Tooltip("How high the camera's curve between two points bows up, as a share of their distance.")]
        [SerializeField, Min(0f)] private float _curveHeightShare = 0.2f;
        [SerializeField, Range(10f, 120f)] private float _fieldOfView = 60f;

        [Header("Debug test cutscene")]
        [Tooltip("The test cutscene's camera stands this far beside the ship.")]
        [SerializeField, Min(1f)] private float _debugSideMeters = 40f;
        [SerializeField, Min(0f)] private float _debugHeightMeters = 15f;
        [Tooltip("Each of its two shots lasts this long.")]
        [SerializeField, Min(0.1f)] private float _debugShotSeconds = 2f;

        public float StartLeadSeconds => _startLeadSeconds;
        public float ReturnBlendSeconds => _returnBlendSeconds;
        public float TurnSeconds => _turnSeconds;
        public float CurveHeightShare => _curveHeightShare;
        public float FieldOfView => _fieldOfView;
        public float DebugSideMeters => _debugSideMeters;
        public float DebugHeightMeters => _debugHeightMeters;
        public float DebugShotSeconds => _debugShotSeconds;
    }
}
