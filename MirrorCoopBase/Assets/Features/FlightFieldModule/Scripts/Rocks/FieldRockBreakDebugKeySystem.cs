#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.FlightFieldModule.Contracts;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Debug only: F6 on the server (host) breaks the rock nearest to the ship.
    public sealed class FieldRockBreakDebugKeySystem : ITickable {
        private const string LOG_PREFIX = "[FieldRock] F6: ";

        private readonly IFieldRockBreakDebug _fieldRockBreakDebug;

        public FieldRockBreakDebugKeySystem(IFieldRockBreakDebug fieldRockBreakDebug) =>
            _fieldRockBreakDebug = fieldRockBreakDebug;

        public void Tick() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.f6Key.wasPressedThisFrame || !NetworkServer.active)
                return;

            FieldRockBreakResult result = _fieldRockBreakDebug.DebugBreakNearestRock(out FieldRockId id);
            Debug.Log($"{LOG_PREFIX}{result} {id}");
        }
    }
}
#endif
