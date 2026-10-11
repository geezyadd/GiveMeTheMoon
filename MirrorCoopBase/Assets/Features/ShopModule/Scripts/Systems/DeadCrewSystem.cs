using System;
using Features.NetworkModelModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.PlayerLifeModule.Scripts.Generated;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using Zenject;

namespace Features.ShopModule.Scripts.Systems {
    // Keeps DeadCrewModel up to date on this client: a death or revive, a player spawning or leaving, a player going
    // offline. The list comes from the synced PlayerLife records, so a late or returning player sees it too.
    public sealed class DeadCrewSystem : IInitializable, IDisposable {
        private readonly DeadCrewModel _deadCrewModel;
        private readonly IDeadCrewService _deadCrewService;
        private readonly IPlayerLifeQuery _playerLifeQuery;
        private readonly IPlayerBodyRegistry _playerBodyRegistry;
        private readonly IReadOnlyPlayerLifeRegistry _playerLifeRegistry;

        public DeadCrewSystem(
            DeadCrewModel deadCrewModel,
            IDeadCrewService deadCrewService,
            IPlayerLifeQuery playerLifeQuery,
            IPlayerBodyRegistry playerBodyRegistry,
            IReadOnlyPlayerLifeRegistry playerLifeRegistry) {
            _deadCrewModel = deadCrewModel;
            _deadCrewService = deadCrewService;
            _playerLifeQuery = playerLifeQuery;
            _playerBodyRegistry = playerBodyRegistry;
            _playerLifeRegistry = playerLifeRegistry;
        }

        public void Initialize() {
            _playerLifeQuery.OnAlivePlayersChanged += OnCrewChanged;
            _playerBodyRegistry.OnClientBodiesChanged += OnCrewChanged;
            _playerLifeRegistry.OnOnlineChanged += OnOnlineChanged;
            OnCrewChanged();
        }

        public void Dispose() {
            _playerLifeQuery.OnAlivePlayersChanged -= OnCrewChanged;
            _playerBodyRegistry.OnClientBodiesChanged -= OnCrewChanged;
            _playerLifeRegistry.OnOnlineChanged -= OnOnlineChanged;
        }

        private void OnCrewChanged() =>
            _deadCrewModel.SetEntries(_deadCrewService.GetDeadOnlineCrew());

        private void OnOnlineChanged(PlayerKey player, bool isOnline) =>
            OnCrewChanged();
    }
}
