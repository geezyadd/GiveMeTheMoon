using UnityEngine;

namespace Features.CameraModule.Scripts {
    [CreateAssetMenu(menuName = "Game/Camera Catalog", fileName = "CameraCatalog")]
    public sealed class CameraCatalog : ScriptableObject {
        [SerializeField] private string _startupCameraId = CameraIds.TPCamera;
        [SerializeField] private float _defaultBlendSeconds = 0.45f;
        [SerializeField] private float _minPitch = -70f;
        [SerializeField] private float _maxPitch = 70f;
        [Tooltip("The third-person camera's field of view is the first-person one (a player setting) plus this, in degrees.")]
        [SerializeField] private float _thirdPersonFieldOfViewOffset = 20f;
        [SerializeField] private GameCamera _fpCameraPrefab;
        [SerializeField] private GameCamera _tpCameraPrefab;
        [Tooltip("The cameras' root: the output camera (main camera, audio listener, Cinemachine brain in manual " +
                 "update).")]
        [SerializeField] private GameObject _cameraRootPrefab;
        [Tooltip("The look pivot the cameras follow; it rides on the player's eye.")]
        [SerializeField] private CameraLookRig _lookPivotPrefab;

        public string StartupCameraId =>
            string.IsNullOrEmpty(_startupCameraId) ? CameraIds.TPCamera : _startupCameraId;

        public float DefaultBlendSeconds => _defaultBlendSeconds;
        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;
        public float ThirdPersonFieldOfViewOffset => _thirdPersonFieldOfViewOffset;
        public GameCamera FpCameraPrefab => _fpCameraPrefab;
        public GameCamera TpCameraPrefab => _tpCameraPrefab;
        public GameObject CameraRootPrefab => _cameraRootPrefab;
        public CameraLookRig LookPivotPrefab => _lookPivotPrefab;
    }
}
