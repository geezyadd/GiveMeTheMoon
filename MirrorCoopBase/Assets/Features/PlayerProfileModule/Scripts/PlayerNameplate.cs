using Features.CameraModule.Scripts.Services;
using Features.PlayerProfileModule.Data.Generated;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace Features.PlayerProfileModule.Scripts {
    // Shows another player's name over their head. A child of the player object, so it moves with the drawn player.
    public sealed class PlayerNameplate : MonoBehaviour {
        [SerializeField] private TextMeshPro _text;
        [Tooltip("The nameplate is shown only while this renderer is, so it hides together with a dead player's body.")]
        [SerializeField] private Renderer _bodyRenderer;

        private PlayerNameplateConfiguration _playerNameplateConfiguration;
        private IPlayerNameplateLayout _playerNameplateLayout;
        private IGameCameraService _gameCameraService;
        private IReadOnlyPlayerProfileModel _profile;

        [Inject]
        private void InjectDependencies(
            PlayerNameplateConfiguration playerNameplateConfiguration,
            IPlayerNameplateLayout playerNameplateLayout,
            IGameCameraService gameCameraService) {
            _playerNameplateConfiguration = playerNameplateConfiguration;
            _playerNameplateLayout = playerNameplateLayout;
            _gameCameraService = gameCameraService;
        }

        private void OnEnable() =>
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

        private void OnDisable() =>
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        public void Show(IReadOnlyPlayerProfileModel profile) {
            Hide();
            ApplyConfiguration();
            _profile = profile;
            _profile.OnDisplayNameChanged += OnDisplayNameChanged;
            _text.text = _profile.DisplayName;
        }

        public void Hide() {
            if (_profile == null)
                return;

            _profile.OnDisplayNameChanged -= OnDisplayNameChanged;
            _profile = null;
            _text.enabled = false;
        }

        private void ApplyConfiguration() {
            // A name like "<size=500>" must stay plain text.
            _text.richText = false;
            _text.fontSize = _playerNameplateConfiguration.FontSize;
            transform.localPosition = Vector3.up * _playerNameplateConfiguration.HeightAboveFeet;
        }

        // Runs after the camera got its final pose for the frame, so the nameplate faces it without a frame of lag.
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera) {
            if (_profile == null || renderingCamera != _gameCameraService.OutputCamera)
                return;

            Transform cameraTransform = renderingCamera.transform;
            float distance = Vector3.Distance(transform.position, cameraTransform.position);
            float alpha = _playerNameplateLayout.CalculateAlpha(distance);
            bool isVisible = _bodyRenderer.enabled && alpha > 0f;
            if (_text.enabled != isVisible)
                _text.enabled = isVisible;

            if (isVisible == false)
                return;

            _text.alpha = alpha;
            transform.rotation = cameraTransform.rotation;
            transform.localScale = Vector3.one * _playerNameplateLayout.CalculateScale(distance);
        }

        private void OnDisplayNameChanged() =>
            _text.text = _profile.DisplayName;
    }
}
