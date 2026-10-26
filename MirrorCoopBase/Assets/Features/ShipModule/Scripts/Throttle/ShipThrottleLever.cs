using Features.ShipModule.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Throttle {
    // The throttle lever's network side on the ship: the synced throttle (ShipThrottle model) and the server's entry
    // points for its holder, the ship's resets and the debug window.
    public sealed class ShipThrottleLever : ShipThrottleBridge {
        [SerializeField] private ShipBase _ship;

        private IReadOnlyShipThrottleModel _shipThrottleModel;
        private IReadOnlyShipRunModel _shipRunModel;
        private IShipThrottleLaunchRule _shipThrottleLaunchRule;
        private ShipThrottleServerControl _serverControl;

        [Inject]
        private void InjectDependencies(
            IReadOnlyShipThrottleModel shipThrottleModel,
            IReadOnlyShipRunModel shipRunModel,
            IShipThrottleLaunchRule shipThrottleLaunchRule) {
            _shipThrottleModel = shipThrottleModel;
            _shipRunModel = shipRunModel;
            _shipThrottleLaunchRule = shipThrottleLaunchRule;
        }

        public override void OnStartServer() {
            base.OnStartServer();
            _serverControl = new ShipThrottleServerControl(
                _ship,
                this,
                _shipThrottleModel,
                _shipRunModel,
                _shipThrottleLaunchRule);
        }

        public override void OnStopServer() =>
            _serverControl = null;

        [Server]
        public bool ServerTryTakeHold(string playerKey) =>
            _serverControl.TryTakeHold(playerKey);

        [Server]
        public void ServerLetGo(string playerKey) =>
            _serverControl.LetGo(playerKey);

        [Server]
        public void ServerMoveByHolder(string playerKey, float position) =>
            _serverControl.MoveByHolder(playerKey, position);

        // No holder check: the debug window's "Throttle = N" and the loop tests set it as if pushed there.
        [Server]
        public void ServerMove(float position) =>
            _serverControl.Move(position);

        // The ship is at a station again (landing, a new run): the handle goes back to Stop and is let go. The run can
        // reset the ship before this identity has started on the server; the model starts at Stop then anyway.
        [Server]
        public void ServerReset() {
            if (_serverControl != null)
                _serverControl.Reset();
        }
    }
}
