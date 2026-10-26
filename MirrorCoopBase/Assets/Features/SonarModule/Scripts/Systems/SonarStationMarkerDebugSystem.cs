#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.DebugModule.Contracts;
using Features.SonarModule.Data;
using Zenject;

namespace Features.SonarModule.Scripts.Systems {
    // Debug only: the debug window's "Show station" toggle turns this peer's own station marker on and off. A button
    // label cannot change, so the toggle registers again with the label of the new state.
    public sealed class SonarStationMarkerDebugSystem : IInitializable, IDisposable {
        private const string SHOWN_LABEL = "Show station: on";
        private const string HIDDEN_LABEL = "Show station: off";
        private const string DESCRIPTION = "Marks the next station on your screen while the ship flies (on the edge with an arrow when it is off screen); only for you, the crew does not see it.";

        private readonly SonarStationMarkerModel _sonarStationMarkerModel;
        private readonly IDebugActionRegistry _debugActionRegistry;

        private IDisposable _registration;

        public SonarStationMarkerDebugSystem(SonarStationMarkerModel sonarStationMarkerModel, IDebugActionRegistry debugActionRegistry) {
            _sonarStationMarkerModel = sonarStationMarkerModel;
            _debugActionRegistry = debugActionRegistry;
        }

        public void Initialize() {
            _sonarStationMarkerModel.OnIsShownChanged += OnIsShownChanged;
            Register();
        }

        public void Dispose() {
            _sonarStationMarkerModel.OnIsShownChanged -= OnIsShownChanged;
            _registration.Dispose();
        }

        private void Register() {
            string label = _sonarStationMarkerModel.IsShown ? SHOWN_LABEL : HIDDEN_LABEL;
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.SHIP, label, DESCRIPTION, ReadUnavailableReason, ToggleStation));
        }

        private string ReadUnavailableReason() =>
            null;

        private void ToggleStation() =>
            _sonarStationMarkerModel.SetShown(_sonarStationMarkerModel.IsShown == false);

        private void OnIsShownChanged() {
            _registration.Dispose();
            Register();
        }
    }
}
#endif
