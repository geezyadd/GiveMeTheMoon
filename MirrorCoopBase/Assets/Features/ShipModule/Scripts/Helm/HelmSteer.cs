using Features.NetworkModelModule.Scripts;

namespace Features.ShipModule.Scripts.Helm {
    // The helm's steer on one peer. A pilot on the server's own peer steers with its stick at once; a pilot on a client
    // sends its stick at most once per interval, and the server flies that pilot's last sent steer. The sent steer
    // belongs to its pilot: a change of occupant clears it, so a ship never keeps turning on a pilot who has left.
    public sealed class HelmSteer {
        private PlayerKey _occupant;
        private bool _isOccupantLocal;
        private float _received;
        private double _nextSendTime;

        public PlayerKey Occupant => _occupant;
        public bool HasPilot => _occupant.IsEmpty == false;
        public bool IsOccupantLocal => _isOccupantLocal;

        public void Enter(PlayerKey occupant) {
            Exit();
            _occupant = occupant;
        }

        // The occupant's player object is on this peer now.
        public void TakeOccupant(bool isLocal) {
            _isOccupantLocal = isLocal;
            _nextSendTime = 0d;
        }

        public void Exit() {
            _occupant = default;
            _isOccupantLocal = false;
            _received = 0f;
        }

        // Only the occupant's peer sends (ShipSeats routes by the sender's seat), so it is the occupant's steer.
        public void ServerReceive(float value) =>
            _received = value;

        public float Read(float localStick) {
            if (HasPilot == false)
                return 0f;

            return _isOccupantLocal ? localStick : _received;
        }

        // A client pilot's turn to send its stick; the host's flight reads the stick itself and sends nothing.
        public bool TryTakeSendTurn(bool isServer, double nowSeconds, float intervalSeconds) {
            if (_isOccupantLocal == false || isServer || nowSeconds < _nextSendTime)
                return false;

            _nextSendTime = nowSeconds + intervalSeconds;
            return true;
        }
    }
}
