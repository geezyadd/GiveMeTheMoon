using Features.ShipModule.Scripts.Generated;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The ShipStats bridge as a component on the ship's network identity: a generated bridge file cannot be added
    // to a GameObject itself.
    public sealed class ShipStatsSync : ShipStatsBridge {
        [SerializeField] private ShipBase _ship;

        // A run starts with the server spawning the ship: publish the base stats before any module is installed.
        public override void OnStartServer() {
            base.OnStartServer();
            _ship.Modules.ServerPublishStats();
        }
    }
}
