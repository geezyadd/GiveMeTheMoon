using System;
using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;
using Features.PlayerLifeModule.Scripts.Generated;
using UnityEngine;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    // Read side on every peer: the local state comes from the PlayerLife registry, the alive targets from the bodies spawned here.
    public sealed class PlayerLifeQuery : IPlayerLifeQuery, IInitializable, IDisposable {
        private readonly IPlayerBodyRegistry _playerBodyRegistry;
        private readonly IReadOnlyPlayerLifeRegistry _playerLifeRegistry;
        private readonly List<IReadOnlyPlayerLifeModel> _subscribed = new();
        private IReadOnlyPlayerLifeModel _localLife;
        private List<Transform> _aliveTargets = new();
        private List<Transform> _scratchTargets = new();

        public PlayerLifeQuery(IPlayerBodyRegistry playerBodyRegistry, IReadOnlyPlayerLifeRegistry playerLifeRegistry) {
            _playerBodyRegistry = playerBodyRegistry;
            _playerLifeRegistry = playerLifeRegistry;
        }

        public PlayerLifeState LocalState =>
            ReadState(_playerLifeRegistry.Local);

        public IReadOnlyList<Transform> AlivePlayerTargets =>
            _aliveTargets;

        public event Action<PlayerLifeState> OnLocalStateChanged;
        public event Action OnAlivePlayersChanged;

        public void Initialize() {
            _playerBodyRegistry.OnClientBodiesChanged += OnClientBodiesChanged;
            _playerLifeRegistry.OnPlayerRemoved += OnLifeRecordRemoved;
            OnClientBodiesChanged();
        }

        public void Dispose() {
            _playerBodyRegistry.OnClientBodiesChanged -= OnClientBodiesChanged;
            _playerLifeRegistry.OnPlayerRemoved -= OnLifeRecordRemoved;
            Unsubscribe();
            SwitchLocalLife(null);
        }

        private static PlayerLifeState ReadState(IReadOnlyPlayerLifeModel life) =>
            life != null ? life.LifeState : PlayerLifeState.Alive;

        private void OnClientBodiesChanged() {
            Unsubscribe();
            IReadOnlyList<PlayerLifeBody> bodies = _playerBodyRegistry.ClientBodies;
            for (int i = 0; i < bodies.Count; i++) {
                IReadOnlyPlayerLifeModel life = bodies[i].Life;
                life.OnLifeStateChanged += OnLifeStateChanged;
                _subscribed.Add(life);
            }

            SwitchLocalLife(_playerLifeRegistry.Local);
            RefreshAliveTargets();
        }

        private void OnLifeRecordRemoved(PlayerKey key) =>
            SwitchLocalLife(_playerLifeRegistry.Local);

        private void OnLifeStateChanged() =>
            RefreshAliveTargets();

        private void OnLocalLifeStateChanged() =>
            OnLocalStateChanged?.Invoke(ReadState(_localLife));

        // The registry has no "local changed" event: the local record is re-read whenever the spawned bodies or records change.
        private void SwitchLocalLife(IReadOnlyPlayerLifeModel next) {
            if (next == _localLife)
                return;

            PlayerLifeState previousState = ReadState(_localLife);
            if (_localLife != null)
                _localLife.OnLifeStateChanged -= OnLocalLifeStateChanged;

            _localLife = next;
            if (_localLife != null)
                _localLife.OnLifeStateChanged += OnLocalLifeStateChanged;

            PlayerLifeState nextState = ReadState(_localLife);
            if (nextState != previousState)
                OnLocalStateChanged?.Invoke(nextState);
        }

        private void Unsubscribe() {
            for (int i = 0; i < _subscribed.Count; i++)
                _subscribed[i].OnLifeStateChanged -= OnLifeStateChanged;

            _subscribed.Clear();
        }

        private void RefreshAliveTargets() {
            _scratchTargets.Clear();
            IReadOnlyList<PlayerLifeBody> bodies = _playerBodyRegistry.ClientBodies;
            for (int i = 0; i < bodies.Count; i++) {
                PlayerLifeBody body = bodies[i];
                if (body.Life.LifeState == PlayerLifeState.Alive)
                    _scratchTargets.Add(body.FollowTarget);
            }

            if (SameTargets(_scratchTargets, _aliveTargets))
                return;

            (_aliveTargets, _scratchTargets) = (_scratchTargets, _aliveTargets);
            OnAlivePlayersChanged?.Invoke();
        }

        private static bool SameTargets(List<Transform> left, List<Transform> right) {
            if (left.Count != right.Count)
                return false;

            for (int i = 0; i < left.Count; i++) {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }
    }
}
