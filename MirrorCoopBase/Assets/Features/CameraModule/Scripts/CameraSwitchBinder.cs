using System;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts.Models;
using Features.InputModule.Realization.Scripts.Generated;
using Zenject;

namespace Features.CameraModule.Scripts {
    public sealed class CameraSwitchBinder : IInitializable, IDisposable {
        private readonly IInputService _input;
        private readonly IGameCameraService _cameras;
        private readonly PlayerControlBlockModel _controlBlockModel;

        public CameraSwitchBinder(
            IInputService input,
            IGameCameraService cameras,
            PlayerControlBlockModel controlBlockModel) {
            _input = input;
            _cameras = cameras;
            _controlBlockModel = controlBlockModel;
        }

        public void Initialize() {
            _input.SwitchCamera.Performed += OnSwitchCamera;
        }

        public void Dispose() {
            _input.SwitchCamera.Performed -= OnSwitchCamera;
        }

        private void OnSwitchCamera() {
            if (_controlBlockModel.IsBlocked)
                return;

            _cameras.ToggleFpAndTp();
        }
    }
}
