using System;
using Features.CameraModule.Scripts.Services;
using Zenject;

namespace Features.GrabModule.Scripts {
    // Moves held items to their hands right after the camera look is applied and before the frame is rendered.
    public sealed class HeldItemFollowSystem : IInitializable, IDisposable {
        private readonly IGameCameraService _gameCameraService;
        private readonly HeldItemRegistry _heldItems;

        public HeldItemFollowSystem(IGameCameraService gameCameraService, HeldItemRegistry heldItems) {
            _gameCameraService = gameCameraService;
            _heldItems = heldItems;
        }

        public void Initialize() {
            _gameCameraService.LookApplied += OnLookApplied;
        }

        public void Dispose() {
            _gameCameraService.LookApplied -= OnLookApplied;
        }

        private void OnLookApplied() {
            _heldItems.FollowHolders();
        }
    }
}
