using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.SonarModule.Data.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.SonarModule.Scripts.Network {
    // The radar module's screen as an interactable: E on it seats the player at the sonar console. It exists only
    // while a radar module stands in the socket and the ship flies; at a station the same screen is the module, and
    // E there takes it off as before.
    public sealed class SonarConsoleInteractable : InteractableBase {
        [SerializeField] private ShipSocket _socket;
        [SerializeField] private SonarConsoleSync _sonarConsoleSync;
        [SerializeField] private Collider _screenCollider;

        private IReadOnlyShipRunModel _shipRunModel;
        private IReadOnlyShipSocketsModel _shipSocketsModel;
        private IReadOnlySonarConsoleModel _sonarConsoleModel;

        public bool IsConsoleActive =>
            _socket.IsOccupied && _shipRunModel.Phase.IsFlight();

        [Inject]
        private void InjectDependencies(
            IReadOnlyShipRunModel shipRunModel,
            IReadOnlyShipSocketsModel shipSocketsModel,
            IReadOnlySonarConsoleModel sonarConsoleModel) {
            _shipRunModel = shipRunModel;
            _shipSocketsModel = shipSocketsModel;
            _sonarConsoleModel = sonarConsoleModel;
        }

        private void Awake() =>
            _screenCollider.enabled = false;

        public override void OnStartClient() {
            _shipRunModel.OnPhaseChanged += RefreshScreenCollider;
            _shipSocketsModel.OnStatesChanged += RefreshScreenCollider;
            _shipSocketsModel.OnAvailableChanged += RefreshScreenCollider;
            RefreshScreenCollider();
        }

        public override void OnStopClient() {
            _shipRunModel.OnPhaseChanged -= RefreshScreenCollider;
            _shipSocketsModel.OnStatesChanged -= RefreshScreenCollider;
            _shipSocketsModel.OnAvailableChanged -= RefreshScreenCollider;
            _screenCollider.enabled = false;
        }

        // The server checks the navigator by player key; here only "the console is free" for the aim.
        public override bool CanUse(NetworkIdentity user, GrabController grab) =>
            IsConsoleActive && grab.IsHandFree && string.IsNullOrEmpty(_sonarConsoleModel.NavigatorKey);

        public override void ServerUse(NetworkIdentity user, GrabController grab) =>
            _sonarConsoleSync.ServerRequestEnter(user.connectionToClient, IsConsoleActive);

        private void RefreshScreenCollider() =>
            _screenCollider.enabled = IsConsoleActive;
    }
}
