using System;
using System.Collections.Generic;
using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GameCoreModule.Contracts;
using Features.InputModule.Realization.Scripts.Generated;
using UnityEngine;
using Zenject;

namespace Features.PlayerLifeModule.Scripts.Spectator {
    public sealed class SpectatorSystem : IInitializable, IDisposable, ITickable, IGameplaySession {
        private const int PREVIOUS_DIRECTION = -1;
        private const int NEXT_DIRECTION = 1;

        private readonly IPlayerLifeQuery _life;
        private readonly IInputService _input;
        private readonly IGameCameraService _cameras;
        private readonly GameCameraModel _cameraModel;
        private readonly SpectatorModel _spectatorModel;
        private readonly PlayerControlBlockModel _controlBlockModel;
        private readonly ISpectatorTargetService _targetService;
        private readonly List<Transform> _targets = new();

        private Transform _savedFollow;
        private Transform _savedLookAt;
        private Transform _savedEye;
        private string _savedCameraId;
        private bool _isSpectateRequested;

        private bool IsTargetLost =>
            _spectatorModel.IsSpectating
            && _spectatorModel.IsWaitingForTargets == false
            && _spectatorModel.Target == null;

        public SpectatorSystem(
            IPlayerLifeQuery life,
            IInputService input,
            IGameCameraService cameras,
            GameCameraModel cameraModel,
            SpectatorModel spectatorModel,
            PlayerControlBlockModel controlBlockModel,
            ISpectatorTargetService targetService) {
            _life = life;
            _input = input;
            _cameras = cameras;
            _cameraModel = cameraModel;
            _spectatorModel = spectatorModel;
            _controlBlockModel = controlBlockModel;
            _targetService = targetService;
        }

        public void Initialize() {
            _life.OnLocalStateChanged += OnLocalStateChanged;
            _life.OnAlivePlayersChanged += OnAlivePlayersChanged;
            _input.SpectatePrev.Performed += OnSpectatePrev;
            _input.SpectateNext.Performed += OnSpectateNext;
            _input.DisableSpectatorMap();
            RequestSpectateIfDead();
        }

        public void Dispose() {
            _life.OnLocalStateChanged -= OnLocalStateChanged;
            _life.OnAlivePlayersChanged -= OnAlivePlayersChanged;
            _input.SpectatePrev.Performed -= OnSpectatePrev;
            _input.SpectateNext.Performed -= OnSpectateNext;
            StopSpectate(false);
        }

        public void Tick() {
            if (_isSpectateRequested)
                EnterRequestedSpectate();

            if (IsTargetLost)
                ApplyTargets();
        }

        public void CleanupGameplay() {
            _isSpectateRequested = false;
            StopSpectate(false);
        }

        // A player who is still dead when the session restarts goes straight back to spectating.
        public void RestartGameplay() {
            StopSpectate(true);
            RequestSpectateIfDead();
        }

        // Spectating starts on the next tick, not inside the state callback: a player who spawns dead reports Dead before
        // its own camera anchor binds the cameras to it in the same spawn, which would take the camera off the target.
        private void RequestSpectateIfDead() =>
            _isSpectateRequested = _life.LocalState == PlayerLifeState.Dead;

        private void EnterRequestedSpectate() {
            _isSpectateRequested = false;
            if (_life.LocalState == PlayerLifeState.Dead)
                EnterSpectate();
        }

        private void EnterSpectate() {
            if (_spectatorModel.IsSpectating)
                return;

            RememberPlayerCamera();
            _controlBlockModel.Request(this);
            _input.EnableSpectatorMap();
            _cameras.BlendTo(CameraIds.TPCamera);
            _spectatorModel.Begin();
            ApplyTargets();
        }

        private void StopSpectate(bool restoreCamera) {
            _controlBlockModel.Release(this);
            _input.DisableSpectatorMap();
            _targets.Clear();
            if (_spectatorModel.IsSpectating == false)
                return;

            _spectatorModel.End();
            if (restoreCamera)
                RestorePlayerCamera();

            ForgetPlayerCamera();
        }

        private void Step(int direction) {
            if (_spectatorModel.IsSpectating == false || _targets.Count == 0)
                return;

            Focus(_targetService.Step(_spectatorModel.TargetIndex, direction, _targets.Count));
        }

        private void ApplyTargets() {
            _targetService.CollectTargets(_life.AlivePlayerTargets, _targets);
            if (_targets.Count == 0) {
                WaitForTargets();
                return;
            }

            Focus(_targetService.ResolveIndex(_targets, _spectatorModel.Target, _spectatorModel.TargetIndex));
        }

        private void Focus(int index) {
            Transform target = _targets[index];
            if (target != _spectatorModel.Target)
                _cameras.RetargetOrbit(target);

            _spectatorModel.Focus(target, index, _targetService.BuildTargetLabel(target, index));
        }

        private void WaitForTargets() {
            if (_spectatorModel.IsWaitingForTargets == false)
                _cameras.HoldOrbit();

            _spectatorModel.WaitForTargets(_targetService.BuildNoTargetsLabel());
        }

        private void RememberPlayerCamera() {
            _savedFollow = _cameraModel.Follow;
            _savedLookAt = _cameraModel.LookAt;
            _savedEye = _cameraModel.Eye;
            _savedCameraId = _cameraModel.ActiveId;
        }

        private void RestorePlayerCamera() {
            if (_savedFollow == null)
                return;

            _cameras.BindToLocalPlayer(_savedFollow, _savedLookAt, _savedEye);
            if (string.IsNullOrEmpty(_savedCameraId) == false)
                _cameras.BlendTo(_savedCameraId);
        }

        private void ForgetPlayerCamera() {
            _savedFollow = null;
            _savedLookAt = null;
            _savedEye = null;
            _savedCameraId = null;
        }

        private void OnLocalStateChanged(PlayerLifeState state) {
            if (state == PlayerLifeState.Dead) {
                _isSpectateRequested = true;
                return;
            }

            _isSpectateRequested = false;
            StopSpectate(true);
        }

        private void OnAlivePlayersChanged() {
            if (_spectatorModel.IsSpectating)
                ApplyTargets();
        }

        private void OnSpectatePrev() =>
            Step(PREVIOUS_DIRECTION);

        private void OnSpectateNext() =>
            Step(NEXT_DIRECTION);
    }
}
