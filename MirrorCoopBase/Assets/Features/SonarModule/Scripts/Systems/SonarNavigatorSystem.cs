using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.SonarModule.Data;
using Features.SonarModule.Data.Generated;
using Features.SonarModule.Scripts.Configurations;
using Features.SonarModule.Scripts.Core;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.SonarModule.Scripts.Systems {
    // Server: seats one navigator at the console by player key and frees the console when they leave it, walk away,
    // disconnect or the ship lands.
    public sealed class SonarNavigatorSystem : ISonarNavigatorSystem, ITickable {
        private readonly IPlayerIdentityService _playerIdentityService;
        private readonly ISonarConsoleRule _sonarConsoleRule;
        private readonly ISonarFlightRule _sonarFlightRule;
        private readonly IReadOnlySonarConsoleModel _sonarConsoleModel;
        private readonly SonarConsoleBindingModel _sonarConsoleBindingModel;
        private readonly IReadOnlyShipRunModel _shipRunModel;
        private readonly SonarConfiguration _sonarConfiguration;

        private float _nextCheckAt;

        public SonarNavigatorSystem(
            IPlayerIdentityService playerIdentityService,
            ISonarConsoleRule sonarConsoleRule,
            ISonarFlightRule sonarFlightRule,
            IReadOnlySonarConsoleModel sonarConsoleModel,
            SonarConsoleBindingModel sonarConsoleBindingModel,
            IReadOnlyShipRunModel shipRunModel,
            SonarConfiguration sonarConfiguration) {
            _playerIdentityService = playerIdentityService;
            _sonarConsoleRule = sonarConsoleRule;
            _sonarFlightRule = sonarFlightRule;
            _sonarConsoleModel = sonarConsoleModel;
            _sonarConsoleBindingModel = sonarConsoleBindingModel;
            _shipRunModel = shipRunModel;
            _sonarConfiguration = sonarConfiguration;
        }

        private bool HasNavigator =>
            string.IsNullOrEmpty(_sonarConsoleModel.NavigatorKey) == false;

        public void ServerEnter(ISonarConsoleWriter writer, NetworkConnectionToClient connection, bool isConsoleActive) {
            string playerKey = _playerIdentityService.GetKey(connection).Id;
            SonarConsoleEnterResult result = _sonarConsoleRule.EvaluateEnter(_sonarConsoleModel.NavigatorKey, playerKey, isConsoleActive);
            if (result != SonarConsoleEnterResult.Entered && result != SonarConsoleEnterResult.AlreadyNavigator)
                return;

            writer.ServerSetNavigatorKey(playerKey);
            writer.ServerOpenConsole(connection, playerKey);
        }

        public void ServerLeave(ISonarConsoleWriter writer, NetworkConnectionToClient connection) {
            if (_playerIdentityService.GetKey(connection).Id == _sonarConsoleModel.NavigatorKey)
                writer.ServerSetNavigatorKey(string.Empty);
        }

        public void Tick() {
            if (NetworkServer.active == false || _sonarConsoleBindingModel.IsBound == false || HasNavigator == false)
                return;

            if (Time.time < _nextCheckAt)
                return;

            _nextCheckAt = Time.time + _sonarConfiguration.ServerCheckSeconds;
            if (IsNavigatorAtConsole() == false)
                _sonarConsoleBindingModel.Writer.ServerSetNavigatorKey(string.Empty);
        }

        private bool IsNavigatorAtConsole() {
            if (_sonarFlightRule.IsFlying(_shipRunModel.Phase) == false || TryFindNavigatorConnection(out NetworkConnectionToClient connection) == false)
                return false;

            return connection.identity != null
                && _sonarConsoleRule.IsWithinReach(connection.identity.transform.position, _sonarConsoleBindingModel.Console.position);
        }

        private bool TryFindNavigatorConnection(out NetworkConnectionToClient navigator) {
            navigator = null;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values) {
                if (_playerIdentityService.GetKey(connection).Id != _sonarConsoleModel.NavigatorKey)
                    continue;

                navigator = connection;
                return true;
            }

            return false;
        }
    }
}
