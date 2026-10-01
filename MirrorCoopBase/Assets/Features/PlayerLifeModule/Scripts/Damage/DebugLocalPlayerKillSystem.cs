#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    // Debug only: F9 kills the local player through the server, the same path as a real death.
    public sealed class DebugLocalPlayerKillSystem : ITickable {
        private readonly IPlayerBodyRegistry _playerBodyRegistry;

        public DebugLocalPlayerKillSystem(IPlayerBodyRegistry playerBodyRegistry) =>
            _playerBodyRegistry = playerBodyRegistry;

        public void Tick() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || keyboard.f9Key.wasPressedThisFrame == false)
                return;

            PlayerLifeBody local = _playerBodyRegistry.LocalBody;
            if (local == null)
                return;

            local.CmdDebugKill();
        }
    }
}
#endif
