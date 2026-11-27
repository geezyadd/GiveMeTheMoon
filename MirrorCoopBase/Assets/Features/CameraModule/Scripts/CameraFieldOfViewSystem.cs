using System;
using Features.CameraModule.Scripts.Models;
using Zenject;

namespace Features.CameraModule.Scripts {
    // Keeps the game cameras' field of view on the model's value: the first-person camera takes it, the third-person
    // camera takes it plus the catalog's offset. Applied when the value changes and to every camera that spawns.
    public sealed class CameraFieldOfViewSystem : IInitializable, IDisposable {
        private readonly CameraFieldOfViewModel _fieldOfViewModel;
        private readonly GameCameraModel _gameCameraModel;
        private readonly CameraCatalog _catalog;

        public CameraFieldOfViewSystem(
            CameraFieldOfViewModel fieldOfViewModel,
            GameCameraModel gameCameraModel,
            CameraCatalog catalog) {
            _fieldOfViewModel = fieldOfViewModel;
            _gameCameraModel = gameCameraModel;
            _catalog = catalog;
        }

        public void Initialize() {
            _fieldOfViewModel.OnChanged += ApplyToAll;
            _gameCameraModel.OnCameraRegistered += Apply;
            ApplyToAll();
        }

        public void Dispose() {
            _fieldOfViewModel.OnChanged -= ApplyToAll;
            _gameCameraModel.OnCameraRegistered -= Apply;
        }

        private void ApplyToAll() {
            foreach (GameCamera camera in _gameCameraModel.Cameras.Values)
                Apply(camera);
        }

        private void Apply(GameCamera camera) {
            if (camera == null || _fieldOfViewModel.IsSet == false)
                return;

            float fieldOfView = _fieldOfViewModel.FirstPersonFieldOfView;
            if (camera.Kind == GameCameraKind.ThirdPerson)
                fieldOfView += _catalog.ThirdPersonFieldOfViewOffset;

            camera.SetFieldOfView(fieldOfView);
        }
    }
}
