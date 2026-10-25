using Features.ShipModule.Scripts.Generated;

namespace Features.ShipModule.Scripts.Throttle {
    // Server: one holder of the handle at a time by player key; only the holder moves it, and every move goes through
    // the launch rule, so a push past the launch mark at a station takes off.
    public sealed class ShipThrottleServerControl {
        private readonly ShipBase _ship;
        private readonly ShipThrottleBridge _shipThrottleBridge;
        private readonly IReadOnlyShipThrottleModel _shipThrottleModel;
        private readonly IReadOnlyShipRunModel _shipRunModel;
        private readonly IShipThrottleLaunchRule _shipThrottleLaunchRule;

        public ShipThrottleServerControl(
            ShipBase ship,
            ShipThrottleBridge shipThrottleBridge,
            IReadOnlyShipThrottleModel shipThrottleModel,
            IReadOnlyShipRunModel shipRunModel,
            IShipThrottleLaunchRule shipThrottleLaunchRule) {
            _ship = ship;
            _shipThrottleBridge = shipThrottleBridge;
            _shipThrottleModel = shipThrottleModel;
            _shipRunModel = shipRunModel;
            _shipThrottleLaunchRule = shipThrottleLaunchRule;
        }

        public bool TryTakeHold(string playerKey) {
            if (string.IsNullOrEmpty(playerKey) || IsHeldByAnother(playerKey))
                return false;

            _shipThrottleBridge.ServerSetHolderKey(playerKey);
            return true;
        }

        public void LetGo(string playerKey) {
            if (string.IsNullOrEmpty(playerKey) == false && _shipThrottleModel.HolderKey == playerKey)
                _shipThrottleBridge.ServerSetHolderKey(string.Empty);
        }

        public void MoveByHolder(string playerKey, float position) {
            if (string.IsNullOrEmpty(playerKey) == false && _shipThrottleModel.HolderKey == playerKey)
                Move(position);
        }

        // A launch the run refuses at the last moment leaves the handle at Stop, as a station that cannot launch does.
        public void Move(float position) {
            ShipThrottleMove move = _shipThrottleLaunchRule.Evaluate(_shipRunModel.Phase, _ship.CanLaunch, position);
            if (move.IsLaunch && _ship.ServerRequestLaunch() == false) {
                _shipThrottleBridge.ServerSetPosition(0f);
                return;
            }

            _shipThrottleBridge.ServerSetPosition(move.Position);
        }

        public void Reset() =>
            _shipThrottleBridge.ServerSetState(new ShipThrottleState { Position = 0f, HolderKey = string.Empty });

        private bool IsHeldByAnother(string playerKey) {
            string holderKey = _shipThrottleModel.HolderKey;
            return string.IsNullOrEmpty(holderKey) == false && holderKey != playerKey;
        }
    }
}
