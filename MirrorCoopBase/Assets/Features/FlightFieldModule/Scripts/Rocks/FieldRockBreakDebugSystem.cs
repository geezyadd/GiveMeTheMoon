#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.DebugModule.Contracts;
using Features.FlightFieldModule.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Debug only: the debug window's "Break the nearest rock" action, on the server (host), breaks the rock nearest
    // to the ship.
    public sealed class FieldRockBreakDebugSystem : IInitializable, IDisposable {
        private const string LOG_PREFIX = "[FieldRock] Debug break: ";
        private const string BREAK_LABEL = "Break the nearest rock";
        private const string BREAK_DESCRIPTION = "Breaks the breakable rock of the flight field nearest to the ship, as a weapon or an impact does (the result is logged with [FieldRock]).";

        private readonly IFieldRockBreakDebug _fieldRockBreakDebug;
        private readonly IDebugActionRegistry _debugActionRegistry;

        private IDisposable _registration;

        public FieldRockBreakDebugSystem(IFieldRockBreakDebug fieldRockBreakDebug, IDebugActionRegistry debugActionRegistry) {
            _fieldRockBreakDebug = fieldRockBreakDebug;
            _debugActionRegistry = debugActionRegistry;
        }

        public void Initialize() =>
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.ROCKS, BREAK_LABEL, BREAK_DESCRIPTION, ReadUnavailableReason, BreakNearestRock));

        public void Dispose() =>
            _registration.Dispose();

        private static string ReadUnavailableReason() =>
            NetworkServer.active ? null : DebugActionReasons.HOST_ONLY;

        private void BreakNearestRock() {
            FieldRockBreakResult result = _fieldRockBreakDebug.DebugBreakNearestRock(out FieldRockId id);
            Debug.Log($"{LOG_PREFIX}{result} {id}");
        }
    }
}
#endif
