using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.MovingPlatformModule {
    // A button on a post: aim at it and press the interact key to start or stop the test platform. The server does it.
    public sealed class PlatformToggleButton : InteractableBase {
        [SerializeField] private CirclingPlatform _platform;

        public override bool CanUse(NetworkIdentity user, GrabController grab) =>
            _platform != null;

        public override void ServerUse(NetworkIdentity user, GrabController grab) =>
            _platform.ServerToggle();
    }
}
