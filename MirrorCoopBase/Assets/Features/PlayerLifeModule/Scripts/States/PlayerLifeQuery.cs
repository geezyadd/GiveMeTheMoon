using System;
using System.Collections.Generic;
using Features.PlayerLifeModule.Scripts.Generated;
using UnityEngine;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    // Read side on every peer, from the synced PlayerLife models of the player bodies spawned here.
    public sealed class PlayerLifeQuery : IPlayerLifeQuery, IInitializable, IDisposable {
        private readonly IPlayerBodyRegistry _playerBodyRegistry;
        private readonly List<IReadOnlyPlayerLifeModel> _subscribed = new();
        private List<Transform> _aliveTargets = new();
        private List<Transform> _scratchTargets = new();

        public PlayerLifeQuery(IPlayerBodyRegistry playerBodyRegistry) =>
            _playerBodyRegistry = playerBodyRegistry;

        public PlayerLifeState LocalState { get; private set; } = PlayerLifeState.Alive;

        public IReadOnlyList<Transform> AlivePlayerTargets =>
            _aliveTargets;

        public event Action<PlayerLifeState> OnLocalStateChanged;
        public event Action OnAlivePlayersChanged;

        public void Initialize() {
            _playerBodyRegistry.OnClientBodiesChanged += OnClientBodiesChanged;
            OnClientBodiesChanged();
        }

        public void Dispose() {
            _playerBodyRegistry.OnClientBodiesChanged -= OnClientBodiesChanged;
            Unsubscribe();
        }

        private void OnClientBodiesChanged() {
            Unsubscribe();
            IReadOnlyList<PlayerLifeBody> bodies = _playerBodyRegistry.ClientBodies;
            for (int i = 0; i < bodies.Count; i++) {
                IReadOnlyPlayerLifeModel life = bodies[i].Life;
                life.OnLifeStateChanged += OnLifeStateChanged;
                _subscribed.Add(life);
            }

            Refresh();
        }

        private void OnLifeStateChanged() =>
            Refresh();

        private void Unsubscribe() {
            for (int i = 0; i < _subscribed.Count; i++)
                _subscribed[i].OnLifeStateChanged -= OnLifeStateChanged;

            _subscribed.Clear();
        }

        private void Refresh() {
            RefreshLocalState();
            RefreshAliveTargets();
        }

        private void RefreshLocalState() {
            PlayerLifeBody local = _playerBodyRegistry.LocalBody;
            PlayerLifeState state = local != null ? local.Life.LifeState : PlayerLifeState.Alive;
            if (state == LocalState)
                return;

            LocalState = state;
            OnLocalStateChanged?.Invoke(state);
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
