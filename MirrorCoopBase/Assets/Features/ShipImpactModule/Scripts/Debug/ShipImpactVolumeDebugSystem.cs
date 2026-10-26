#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.DebugModule.Contracts;
using Zenject;

namespace Features.ShipImpactModule.Scripts.Debug {
    // Debug only, any player: the debug window's "Show impact volume" toggle draws the hull boxes and the rocks'
    // contact spheres on this peer. A button label cannot change, so the toggle registers again with the new label.
    public sealed class ShipImpactVolumeDebugSystem : IInitializable, IDisposable {
        private const string SHOWN_LABEL = "Show impact volume: on";
        private const string HIDDEN_LABEL = "Show impact volume: off";
        private const string DESCRIPTION = "Draws what the rock check uses, only for you: the hull boxes (green) and, for the rocks near the ship, the sphere the hull must reach to hit them (small: orange, large: red) inside the rock's data sphere (grey).";

        private readonly IShipImpactVolumeDebugView _shipImpactVolumeDebugView;
        private readonly IDebugActionRegistry _debugActionRegistry;

        private IDisposable _registration;

        public ShipImpactVolumeDebugSystem(IShipImpactVolumeDebugView shipImpactVolumeDebugView, IDebugActionRegistry debugActionRegistry) {
            _shipImpactVolumeDebugView = shipImpactVolumeDebugView;
            _debugActionRegistry = debugActionRegistry;
        }

        public void Initialize() =>
            Register();

        public void Dispose() =>
            _registration.Dispose();

        private void Register() {
            string label = _shipImpactVolumeDebugView.IsShown ? SHOWN_LABEL : HIDDEN_LABEL;
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.ROCKS, label, DESCRIPTION, ReadUnavailableReason, ToggleVolume));
        }

        private string ReadUnavailableReason() =>
            null;

        private void ToggleVolume() {
            _shipImpactVolumeDebugView.SetShown(_shipImpactVolumeDebugView.IsShown == false);
            _registration.Dispose();
            Register();
        }
    }
}
#endif
