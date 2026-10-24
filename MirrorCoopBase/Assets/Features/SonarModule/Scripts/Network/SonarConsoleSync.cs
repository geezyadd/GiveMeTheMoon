using Features.ShipModule.Scripts;
using Features.SonarModule.Data;
using Features.SonarModule.Data.Generated;
using Features.SonarModule.Scripts.Systems;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.SonarModule.Scripts.Network {
    // The console's network side on the radar socket: the synced navigator and marks (SonarConsole model), the
    // navigator's requests to the server and the server's message that seats a navigator on their own peer.
    public sealed class SonarConsoleSync : SonarConsoleBridge, ISonarConsoleWriter {
        [SerializeField] private Transform _consolePoint;
        [SerializeField] private ShipSocket _socket;

        private SonarConsoleBindingModel _sonarConsoleBindingModel;
        private SonarScopeModel _sonarScopeModel;
        private SonarConsoleRequests _sonarConsoleRequests;
        private ISonarNavigatorSystem _sonarNavigatorSystem;
        private ISonarMarkSystem _sonarMarkSystem;

        [Inject]
        private void InjectDependencies(
            SonarConsoleBindingModel sonarConsoleBindingModel,
            SonarScopeModel sonarScopeModel,
            SonarConsoleRequests sonarConsoleRequests,
            ISonarNavigatorSystem sonarNavigatorSystem,
            ISonarMarkSystem sonarMarkSystem) {
            _sonarConsoleBindingModel = sonarConsoleBindingModel;
            _sonarScopeModel = sonarScopeModel;
            _sonarConsoleRequests = sonarConsoleRequests;
            _sonarNavigatorSystem = sonarNavigatorSystem;
            _sonarMarkSystem = sonarMarkSystem;
        }

        public override void OnStartServer() {
            base.OnStartServer();
            _sonarConsoleBindingModel.Bind(this, _consolePoint, _socket.SocketId);
        }

        public override void OnStopServer() =>
            _sonarConsoleBindingModel.Clear(this);

        public override void OnStartClient() {
            base.OnStartClient();
            _sonarConsoleBindingModel.Bind(this, _consolePoint, _socket.SocketId);
            _sonarConsoleRequests.OnMarkRequested += OnMarkRequested;
            _sonarScopeModel.OnOpenChanged += OnScopeOpenChanged;
        }

        public override void OnStopClient() {
            _sonarConsoleRequests.OnMarkRequested -= OnMarkRequested;
            _sonarScopeModel.OnOpenChanged -= OnScopeOpenChanged;
            _sonarScopeModel.Close();
            _sonarConsoleBindingModel.Clear(this);
            base.OnStopClient();
        }

        [Server]
        public void ServerRequestEnter(NetworkConnectionToClient connection, bool isConsoleActive) =>
            _sonarNavigatorSystem.ServerEnter(this, connection, isConsoleActive);

        [Server]
        public void ServerOpenConsole(NetworkConnectionToClient connection, string navigatorKey) =>
            TargetOpenConsole(connection, navigatorKey);

        [TargetRpc]
        private void TargetOpenConsole(NetworkConnectionToClient target, string navigatorKey) =>
            _sonarScopeModel.Open(navigatorKey, Time.frameCount);

        [Command(requiresAuthority = false)]
        private void CmdLeave(NetworkConnectionToClient sender = null) =>
            _sonarNavigatorSystem.ServerLeave(this, sender);

        [Command(requiresAuthority = false)]
        private void CmdMark(string contactId, NetworkConnectionToClient sender = null) =>
            _sonarMarkSystem.ServerMark(this, sender, contactId);

        private void OnMarkRequested(string contactId) =>
            CmdMark(contactId);

        private void OnScopeOpenChanged() {
            if (_sonarScopeModel.IsOpen == false)
                CmdLeave();
        }
    }
}
