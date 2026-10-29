using Features.CameraModule.Contracts;
using Features.CameraModule.Scripts.Services;
using UnityEngine;

namespace Features.CameraModule.Scripts {
    // The local player's view axes from the game's output camera.
    public sealed class CameraViewDirectionSource : ILocalViewDirectionSource {
        private readonly IGameCameraService _gameCameraService;

        public Vector3 Forward => _gameCameraService.OutputCamera.transform.forward;
        public Vector3 Right => _gameCameraService.OutputCamera.transform.right;

        public CameraViewDirectionSource(IGameCameraService gameCameraService) =>
            _gameCameraService = gameCameraService;
    }
}
