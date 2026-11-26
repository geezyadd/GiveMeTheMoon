#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.CharacterMovableModule.Scripts.Drift;
using Features.DebugModule.Contracts;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only, any player, local: the debug window's "Drift speed" slider scales how hard this peer's own drifting
    // body pushes with the move keys (acceleration and top speed), from the configured push (x1) to ten times it.
    public sealed class ShipOverboardDebugSystem : IInitializable, IDisposable {
        private const string DRIFT_SPEED_LABEL = "Drift speed";
        private const string DRIFT_SPEED_DESCRIPTION = "How hard the move keys push you while drifting in open space, times the configured push (acceleration and top speed). This player only.";
        private const string DRIFT_SPEED_VALUE_LABEL = "x";
        private const float MIN_SCALE = 1f;
        private const float MAX_SCALE = 10f;

        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly DriftPushScaleModel _driftPushScaleModel;

        private IDisposable _registration;

        public ShipOverboardDebugSystem(IDebugActionRegistry debugActionRegistry, DriftPushScaleModel driftPushScaleModel) {
            _debugActionRegistry = debugActionRegistry;
            _driftPushScaleModel = driftPushScaleModel;
        }

        public void Initialize() {
            DebugActionValue scale = new(DRIFT_SPEED_VALUE_LABEL, MIN_SCALE, MIN_SCALE, MAX_SCALE, ReadScale);
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.PLAYER, DRIFT_SPEED_LABEL,
                DRIFT_SPEED_DESCRIPTION, ReadAlwaysAvailable, scale, SetScale));
        }

        public void Dispose() =>
            _registration.Dispose();

        private float ReadScale() =>
            _driftPushScaleModel.Scale;

        private void SetScale(float scale) =>
            _driftPushScaleModel.Scale = Math.Clamp(scale, MIN_SCALE, MAX_SCALE);

        private static string ReadAlwaysAvailable() =>
            null;
    }
}
#endif
