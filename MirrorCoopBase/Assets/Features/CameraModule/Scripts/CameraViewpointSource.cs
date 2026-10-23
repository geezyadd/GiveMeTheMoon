using Features.CameraModule.Scripts.Services;
using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.CameraModule.Scripts {
    // The local player's view: the game camera rides the local ship on every peer (also when spectating), while the
    // ship itself is bound to the run only on the server.
    public sealed class CameraViewpointSource : ILocalViewpointSource {
        private readonly IGameCameraService _gameCameraService;

        public CameraViewpointSource(IGameCameraService gameCameraService) =>
            _gameCameraService = gameCameraService;

        public Vector3 GetPosition() =>
            _gameCameraService.OutputCamera.transform.position;
    }
}
