using System;
using Features.GameCoreModule.Contracts;
using Features.SpaceHazeModule.Data;
using Features.SpaceHazeModule.Scripts.Core;
using Features.SpaceHazeModule.Scripts.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Zenject;

namespace Features.SpaceHazeModule.Scripts.Systems {
    // Every peer hazes its own game camera while a run's ship stands (stations and flight), from the same
    // configuration, so every player sees as far as the others. Not in the menu or the lobby.
    public sealed class SpaceHazeSystem : IInitializable, IDisposable {
        private readonly SpaceHazeModel _spaceHazeModel;
        private readonly ISpaceHazeRule _spaceHazeRule;
        private readonly ISpaceHazeRenderer _spaceHazeRenderer;
        private readonly IShipPositionSource _shipPositionSource;

        private Camera _knownCamera;
        private UniversalAdditionalCameraData _knownCameraData;

        public SpaceHazeSystem(SpaceHazeModel spaceHazeModel, ISpaceHazeRule spaceHazeRule, ISpaceHazeRenderer spaceHazeRenderer, IShipPositionSource shipPositionSource) {
            _spaceHazeModel = spaceHazeModel;
            _spaceHazeRule = spaceHazeRule;
            _spaceHazeRenderer = spaceHazeRenderer;
            _shipPositionSource = shipPositionSource;
        }

        public void Initialize() =>
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

        public void Dispose() =>
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        // The camera's URP data is looked up once per camera, not every frame; URP adds it to a camera that has none.
        private bool TryGetBaseCameraData(Camera camera, out UniversalAdditionalCameraData cameraData) {
            if (camera != _knownCamera) {
                _knownCamera = camera;
                _knownCameraData = camera.GetUniversalAdditionalCameraData();
            }

            cameraData = _knownCameraData;
            return cameraData.renderType == CameraRenderType.Base;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera) {
            bool isGameView = camera.cameraType == CameraType.Game && _shipPositionSource.TryGetShipPosition(out _);
            if (isGameView == false)
                return;

            SpaceHazeShape shape = _spaceHazeRule.CreateShape(_spaceHazeModel);
            if (shape.Strength <= 0f || TryGetBaseCameraData(camera, out UniversalAdditionalCameraData cameraData) == false)
                return;

            _spaceHazeRenderer.Enqueue(cameraData.scriptableRenderer, shape);
        }
    }
}
