using System;
using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Features.GameCoreModule.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Features.CameraModule.Scripts.Shake {
    // Applies the sum of all camera shakes to the look pivot once per frame, after the look rig has posed it and
    // before the camera brain reads it, so a shake never adds up over frames. A seated station's view rides the same
    // pivot. The shakes end with the gameplay and on every scene load.
    public sealed class CameraShakeSystem : IInitializable, IDisposable, IGameplaySession {
        private readonly IGameCameraService _gameCameraService;
        private readonly GameCameraModel _gameCameraModel;
        private readonly CameraShakeModel _cameraShakeModel;
        private readonly ICameraShakeSampler _cameraShakeSampler;

        public CameraShakeSystem(
            IGameCameraService gameCameraService,
            GameCameraModel gameCameraModel,
            CameraShakeModel cameraShakeModel,
            ICameraShakeSampler cameraShakeSampler) {
            _gameCameraService = gameCameraService;
            _gameCameraModel = gameCameraModel;
            _cameraShakeModel = cameraShakeModel;
            _cameraShakeSampler = cameraShakeSampler;
        }

        public void Initialize() {
            _gameCameraService.LookApplied += OnLookApplied;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void Dispose() {
            _gameCameraService.LookApplied -= OnLookApplied;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void CleanupGameplay() =>
            _cameraShakeModel.Clear();

        public void RestartGameplay() =>
            _cameraShakeModel.Clear();

        private void OnLookApplied() {
            Transform pivot = _gameCameraModel.LookPivot;
            if (_cameraShakeModel.IsEmpty || pivot == null)
                return;

            _cameraShakeSampler.Advance(_cameraShakeModel, Time.deltaTime);
            CameraShakePose pose = _cameraShakeSampler.Sample(_cameraShakeModel);
            pivot.localRotation *= Quaternion.Euler(pose.EulerDegrees);
            pivot.localPosition += pose.Offset;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>
            _cameraShakeModel.Clear();
    }
}
