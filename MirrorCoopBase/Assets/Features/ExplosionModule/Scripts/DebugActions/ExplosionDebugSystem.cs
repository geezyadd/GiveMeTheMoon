#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.CameraModule.Scripts.Services;
using Features.DebugModule.Contracts;
using Features.ExplosionModule.Configuration;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ExplosionModule.DebugActions {
    // Debug only, on the host: one button per explosion preset sets it off a few metres in front of the host's camera.
    public sealed class ExplosionDebugSystem : IInitializable, IDisposable {
        private const string LABEL_FORMAT = "Explode: {0}";
        private const string DESCRIPTION = "Sets this explosion off in front of your camera: its wave throws items and knocks players down.";
        private const string NO_CAMERA_REASON = "No camera yet.";

        private readonly ExplosionConfiguration _configuration;
        private readonly IExplosionService _explosionService;
        private readonly IGameCameraService _cameraService;
        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly List<IDisposable> _registrations = new();

        public ExplosionDebugSystem(
            ExplosionConfiguration configuration,
            IExplosionService explosionService,
            IGameCameraService cameraService,
            IDebugActionRegistry debugActionRegistry) {
            _configuration = configuration;
            _explosionService = explosionService;
            _cameraService = cameraService;
            _debugActionRegistry = debugActionRegistry;
        }

        public void Initialize() {
            foreach (ExplosionPreset preset in _configuration.Presets) {
                if (preset == null)
                    continue;

                string label = string.Format(LABEL_FORMAT, preset.DisplayName);
                _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(
                    DebugSections.EXPLOSIONS, label, DESCRIPTION, ReadUnavailableReason, () => Explode(preset))));
            }
        }

        public void Dispose() {
            foreach (IDisposable registration in _registrations)
                registration.Dispose();

            _registrations.Clear();
        }

        private string ReadUnavailableReason() {
            if (NetworkServer.active == false)
                return DebugActionReasons.HOST_ONLY;

            return _cameraService.OutputCamera == null ? NO_CAMERA_REASON : null;
        }

        private void Explode(ExplosionPreset preset) {
            Transform view = _cameraService.OutputCamera.transform;
            _explosionService.ServerExplode(view.position + view.forward * _configuration.DebugDistance, preset);
        }
    }
}
#endif
