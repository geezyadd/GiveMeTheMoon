using System;
using Features.CameraModule.Scripts.Services;
using Zenject;

namespace Features.GrabModule.Scripts {
    public sealed class HeldItemFollowSystem : IInitializable, IDisposable {
        private readonly IGameCameraService _gameCameraService;

        public HeldItemFollowSystem(IGameCameraService gameCameraService) {
            _gameCameraService = gameCameraService;
        }

        public void Initialize() {
            _gameCameraService.LookApplied += OnLookApplied;
        }

        public void Dispose() {
            _gameCameraService.LookApplied -= OnLookApplied;
        }

        private void OnLookApplied() {
            Grabbable.FollowHeldAll();
        }
    }
}
