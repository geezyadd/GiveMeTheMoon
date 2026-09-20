using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipLaunchLever : NetworkBehaviour {
        [SerializeField] private ShipBase _ship;
        [SerializeField] private Outline _outline;

        [SyncVar]
        private double _launchNetworkTime;

        internal void SetHovered(bool hovered) {
            if (_outline == null)
                return;

            _outline.enabled = hovered;
        }

        internal bool ServerTryLaunch() {
            if (isServer == false || _ship == null || _launchNetworkTime > 0d)
                return false;

            if (_ship.CanLaunch == false)
                return false;

            if (_ship.ServerRequestLaunch() == false)
                return false;

            _launchNetworkTime = NetworkTime.time;
            return true;
        }

        internal void ServerReset() {
            if (isServer == false)
                return;

            _launchNetworkTime = 0d;
        }
    }
}
