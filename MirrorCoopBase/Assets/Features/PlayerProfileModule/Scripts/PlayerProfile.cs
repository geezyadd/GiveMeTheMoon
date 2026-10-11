using Features.NetworkModelModule.Scripts;
using Features.PlayerProfileModule.Data;
using Features.PlayerProfileModule.Data.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.PlayerProfileModule.Scripts {
    // The player's profile bridge: the server fills the name from the connection's authentication data, every other
    // peer shows it on the nameplate.
    public sealed class PlayerProfile : PlayerProfileBridge, IPlayerProfileSource {
        [SerializeField] private PlayerNameplate _nameplate;

        private IPlayerNameSource _playerNameSource;
        private IPlayerNameSanitizer _playerNameSanitizer;
        private PlayerNameplateConfiguration _playerNameplateConfiguration;

        public IReadOnlyPlayerProfileModel Model =>
            Bound;

        [Inject]
        private void InjectDependencies(
            IPlayerNameSource playerNameSource,
            IPlayerNameSanitizer playerNameSanitizer,
            PlayerNameplateConfiguration playerNameplateConfiguration) {
            _playerNameSource = playerNameSource;
            _playerNameSanitizer = playerNameSanitizer;
            _playerNameplateConfiguration = playerNameplateConfiguration;
        }

        public override void OnStartServer() {
            base.OnStartServer();
            ServerSetDisplayName(_playerNameSanitizer.Sanitize(
                _playerNameSource.GetAuthenticatedName(connectionToClient),
                _playerNameplateConfiguration.MaxNameLength,
                BuildFallbackName()));
        }

        public override void OnStartClient() {
            base.OnStartClient();
            if (isLocalPlayer)
                return;

            _nameplate.Show(Bound);
        }

        public override void OnStopClient() {
            _nameplate.Hide();
            base.OnStopClient();
        }

        // Cosmetic only: the number is the count of connected players when this one spawned, it is not an ID.
        private string BuildFallbackName() =>
            _playerNameplateConfiguration.FallbackNamePrefix + NetworkServer.connections.Count;
    }
}
