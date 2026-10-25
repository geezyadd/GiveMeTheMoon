#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.DebugModule.Contracts;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    // Debug only: the debug window's "Kill me" action kills the local player through the server, the same path as a
    // real death.
    public sealed class DebugLocalPlayerKillSystem : IInitializable, IDisposable {
        private const string KILL_LABEL = "Kill me";
        private const string KILL_DESCRIPTION = "Kills your player through the server, the same path as a real death: you spectate until the crew buys you back.";
        private const string ALREADY_DEAD_REASON = "You are already dead.";

        private readonly IPlayerBodyRegistry _playerBodyRegistry;
        private readonly IPlayerLifeQuery _playerLifeQuery;
        private readonly IDebugActionRegistry _debugActionRegistry;

        private IDisposable _registration;

        public DebugLocalPlayerKillSystem(IPlayerBodyRegistry playerBodyRegistry, IPlayerLifeQuery playerLifeQuery, IDebugActionRegistry debugActionRegistry) {
            _playerBodyRegistry = playerBodyRegistry;
            _playerLifeQuery = playerLifeQuery;
            _debugActionRegistry = debugActionRegistry;
        }

        public void Initialize() =>
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.PLAYER, KILL_LABEL, KILL_DESCRIPTION, ReadUnavailableReason, KillLocalPlayer));

        public void Dispose() =>
            _registration.Dispose();

        private string ReadUnavailableReason() {
            if (_playerBodyRegistry.LocalBody == null)
                return DebugActionReasons.NO_LOCAL_PLAYER;

            return _playerLifeQuery.LocalState == PlayerLifeState.Dead ? ALREADY_DEAD_REASON : null;
        }

        private void KillLocalPlayer() =>
            _playerBodyRegistry.LocalBody.CmdDebugKill();
    }
}
#endif
