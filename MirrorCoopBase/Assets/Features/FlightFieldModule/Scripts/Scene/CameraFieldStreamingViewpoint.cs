using Features.CameraModule.Scripts.Services;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // The local player's view: the game camera rides the local ship on every peer (also when spectating), while the
    // ship itself is bound to the run only on the server.
    public sealed class CameraFieldStreamingViewpoint : IFieldStreamingViewpoint {
        private readonly IGameCameraService _gameCameraService;

        public CameraFieldStreamingViewpoint(IGameCameraService gameCameraService) =>
            _gameCameraService = gameCameraService;

        public Vector3 GetPosition() =>
            _gameCameraService.OutputCamera.transform.position;
    }
}
