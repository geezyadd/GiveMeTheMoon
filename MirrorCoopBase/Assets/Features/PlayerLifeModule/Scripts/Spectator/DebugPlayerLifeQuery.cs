#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.CameraModule.Scripts;
using Features.GameCoreModule.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.PlayerLifeModule.Scripts.Spectator {
    // Debug-only stand-in for IPlayerLifeQuery (editor and development builds). Every spawned player stays Alive.
    // F9 toggles the local player Dead/Alive so the spectator can be tested before the real life states exist.
    public sealed class DebugPlayerLifeQuery : IPlayerLifeQuery, ITickable, IGameplaySession {
        private const float SCAN_INTERVAL = 0.25f;

        private readonly List<Transform> _targets = new();
        private readonly List<Transform> _scan = new();

        private PlayerLifeState _localState = PlayerLifeState.Alive;
        private float _nextScanAt;
        private bool _scanned;

        public PlayerLifeState LocalState => _localState;

        public event Action<PlayerLifeState> OnLocalStateChanged;
        public event Action OnAlivePlayersChanged;

        public IReadOnlyList<Transform> AlivePlayerTargets {
            get {
                if (_scanned == false)
                    RefreshTargets(false);

                return _targets;
            }
        }

        public void Tick() {
            PollDeathToggle();
            if (HasMissingTarget() || Time.time >= _nextScanAt)
                RefreshTargets(true);
        }

        public void CleanupGameplay() =>
            ResetLocalState();

        public void RestartGameplay() =>
            ResetLocalState();

        private void ResetLocalState() =>
            _localState = PlayerLifeState.Alive;

        private void PollDeathToggle() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || keyboard.f9Key.wasPressedThisFrame == false)
                return;

            _localState = _localState == PlayerLifeState.Dead
                ? PlayerLifeState.Alive
                : PlayerLifeState.Dead;
            OnLocalStateChanged?.Invoke(_localState);
        }

        private bool HasMissingTarget() {
            for (int i = 0; i < _targets.Count; i++) {
                if (_targets[i] == null)
                    return true;
            }

            return false;
        }

        private void RefreshTargets(bool notify) {
            _nextScanAt = Time.time + SCAN_INTERVAL;
            _scan.Clear();
            PlayerCameraAnchor[] anchors = UnityEngine.Object.FindObjectsByType<PlayerCameraAnchor>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < anchors.Length; i++) {
                Transform follow = anchors[i].Follow;
                if (follow != null)
                    _scan.Add(follow);
            }

            SortByInstanceId(_scan);
            bool changed = SameTargets(_scan) == false;
            if (changed) {
                _targets.Clear();
                for (int i = 0; i < _scan.Count; i++)
                    _targets.Add(_scan[i]);
            }

            _scanned = true;
            if (notify && changed)
                OnAlivePlayersChanged?.Invoke();
        }

        private bool SameTargets(List<Transform> next) {
            if (next.Count != _targets.Count)
                return false;

            for (int i = 0; i < next.Count; i++) {
                if (next[i] != _targets[i])
                    return false;
            }

            return true;
        }

        private static void SortByInstanceId(List<Transform> list) {
            for (int i = 1; i < list.Count; i++) {
                Transform value = list[i];
                int valueId = value.GetInstanceID();
                int j = i - 1;
                while (j >= 0 && list[j].GetInstanceID() > valueId) {
                    list[j + 1] = list[j];
                    j--;
                }

                list[j + 1] = value;
            }
        }
    }
}
#endif
