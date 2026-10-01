using System;
using System.Collections.Generic;
using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.GameCoreModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using Game.Connection;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.PlayerLifeModule.Scripts.Spectator {
    public sealed class SpectatorSystem : IInitializable, IDisposable, ITickable, IGameplaySession {
        private const string SPECTATING_PREFIX = "Spectating ";
        private const string SPECTATING_HINT = "   [A] / [D]";
        private const string EVERYONE_DEAD_LABEL = "Everyone is dead";
        private const string PLAYER_PREFIX = "Player ";

        private readonly IPlayerLifeQuery _life;
        private readonly IInputService _input;
        private readonly InputActions _actions;
        private readonly IGameCameraService _cameras;
        private readonly GameCameraModel _cameraModel;
        private readonly SpectatorModel _hud;
        private readonly CharacterInputBuffer _inputBuffer;
        private readonly List<Transform> _targets = new();

        private Transform _savedFollow;
        private Transform _savedLookAt;
        private Transform _savedEye;
        private string _savedCameraId;
        private Transform _current;
        private int _index;
        private bool _spectating;
        private bool _holding;

        public SpectatorSystem(
            IPlayerLifeQuery life,
            IInputService input,
            InputActions actions,
            IGameCameraService cameras,
            GameCameraModel cameraModel,
            SpectatorModel hud,
            CharacterInputBuffer inputBuffer) {
            _life = life;
            _input = input;
            _actions = actions;
            _cameras = cameras;
            _cameraModel = cameraModel;
            _hud = hud;
            _inputBuffer = inputBuffer;
        }

        public void Initialize() {
            _life.OnLocalStateChanged += OnLocalStateChanged;
            _life.OnAlivePlayersChanged += OnAlivePlayersChanged;
            _input.SpectatePrev.Performed += OnSpectatePrev;
            _input.SpectateNext.Performed += OnSpectateNext;
            _input.DisableSpectatorMap();
            if (_life.LocalState == PlayerLifeState.Dead)
                EnterSpectate();
        }

        public void Dispose() {
            _life.OnLocalStateChanged -= OnLocalStateChanged;
            _life.OnAlivePlayersChanged -= OnAlivePlayersChanged;
            _input.SpectatePrev.Performed -= OnSpectatePrev;
            _input.SpectateNext.Performed -= OnSpectateNext;
            StopSpectate(false);
        }

        public void CleanupGameplay() {
            if (_life is DebugPlayerLifeQuery debug)
                debug.ResetDebugState();

            StopSpectate(false);
            _hud.Clear();
        }

        public void RestartGameplay() {
            if (_life is DebugPlayerLifeQuery debug)
                debug.ResetDebugState();

            StopSpectate(true);
            _hud.Clear();
        }

        public void Tick() {
            if (_life is DebugPlayerLifeQuery debug)
                debug.DebugTick();

            if (_spectating == false || _holding || _current != null)
                return;

            ApplyCurrentList();
        }

        private void OnLocalStateChanged(PlayerLifeState state) {
            if (state == PlayerLifeState.Dead)
                EnterSpectate();
            else
                StopSpectate(true);
        }

        private void OnAlivePlayersChanged() {
            if (_spectating)
                ApplyCurrentList();
        }

        private void OnSpectatePrev() =>
            Step(-1);

        private void OnSpectateNext() =>
            Step(1);

        private void EnterSpectate() {
            if (_spectating)
                return;

            _spectating = true;
            RememberPlayerCamera();
            SetGameplayBlocked(true);
            _input.EnableSpectatorMap();
            _cameras.BlendTo(CameraIds.TPCamera);
            ApplyCurrentList();
        }

        private void StopSpectate(bool restoreCamera) {
            if (_spectating == false) {
                SetGameplayBlocked(false);
                _input.DisableSpectatorMap();
                _hud.Hide();
                return;
            }

            _spectating = false;
            _holding = false;
            _current = null;
            _index = 0;
            _targets.Clear();
            SetGameplayBlocked(false);
            _input.DisableSpectatorMap();
            if (restoreCamera)
                RestorePlayerCamera();

            ForgetPlayerCamera();
            _hud.Hide();
        }

        private void Step(int direction) {
            if (_spectating == false || _targets.Count == 0)
                return;

            int count = _targets.Count;
            _index = (_index + direction) % count;
            if (_index < 0)
                _index += count;

            Focus(_targets[_index]);
        }

        private void ApplyCurrentList() {
            Transform previous = _current;
            RebuildTargets();
            if (_targets.Count == 0) {
                ShowEmpty();
                return;
            }

            int kept = IndexOf(_targets, previous);
            if (kept >= 0) {
                _index = kept;
                _holding = false;
                PublishLabel();
                return;
            }

            if (_index >= _targets.Count)
                _index = 0;

            Focus(_targets[_index]);
        }

        private void ShowEmpty() {
            _current = null;
            _index = 0;
            if (_holding == false) {
                _cameras.HoldOrbit();
                _holding = true;
            }

            _hud.Show(EVERYONE_DEAD_LABEL);
        }

        private void Focus(Transform target) {
            _holding = false;
            bool changed = target != _current;
            _current = target;
            if (changed)
                _cameras.RetargetOrbit(target);

            PublishLabel();
        }

        private void PublishLabel() {
            if (_index < 0 || _index >= _targets.Count)
                return;

            string name = ResolveName(_targets[_index], _index);
            _hud.Show(SPECTATING_PREFIX + name + SPECTATING_HINT);
        }

        private void RebuildTargets() {
            _targets.Clear();
            IReadOnlyList<Transform> alive = _life.AlivePlayerTargets;
            for (int i = 0; i < alive.Count; i++) {
                Transform target = alive[i];
                if (target == null || BelongsToLocalPlayer(target))
                    continue;

                _targets.Add(target);
            }
        }

        private static bool BelongsToLocalPlayer(Transform target) {
            NetworkIdentity identity = target.GetComponentInParent<NetworkIdentity>();
            return identity != null && identity.isLocalPlayer;
        }

        private static string ResolveName(Transform target, int index) {
            NetworkIdentity identity = target.GetComponentInParent<NetworkIdentity>();
            if (identity != null
                && identity.connectionToClient != null
                && identity.connectionToClient.authenticationData is ConnectionAuthenticator.AuthRequestMessage auth
                && string.IsNullOrWhiteSpace(auth.playerName) == false)
                return auth.playerName;

            return PLAYER_PREFIX + (index + 1).ToString();
        }

        private static int IndexOf(List<Transform> list, Transform target) {
            if (target == null)
                return -1;

            for (int i = 0; i < list.Count; i++) {
                if (list[i] == target)
                    return i;
            }

            return -1;
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

        private void SetGameplayBlocked(bool blocked) {
            if (blocked) {
                _inputBuffer.SetSuppressed(true);
                SetGameplayActionsEnabled(false);
                return;
            }

            _inputBuffer.SetSuppressed(false);
            SetGameplayActionsEnabled(true);
        }

        private void SetGameplayActionsEnabled(bool enabled) {
            SetAction(_actions.MovementMap.Movement, enabled);
            SetAction(_actions.MovementMap.Jump, enabled);
            SetAction(_actions.MovementMap.Sprint, enabled);
            SetAction(_actions.MovementMap.Grab, enabled);
            SetAction(_actions.MovementMap.Release, enabled);
            SetAction(_actions.MovementMap.SwitchCamera, enabled);
        }

        private static void SetAction(InputAction action, bool enabled) {
            if (enabled)
                action.Enable();
            else
                action.Disable();
        }
    }
}
