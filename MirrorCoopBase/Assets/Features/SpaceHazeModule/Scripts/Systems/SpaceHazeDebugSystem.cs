#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.DebugModule.Contracts;
using Features.SpaceHazeModule.Data;
using Features.SpaceHazeModule.Scripts.Configurations;
using Zenject;

namespace Features.SpaceHazeModule.Scripts.Systems {
    // Debug only: the debug window's "Haze: on / off" toggle and "Haze clear distance" for this peer's own view. A
    // button label cannot change, so the toggle registers again with the label of the new state.
    public sealed class SpaceHazeDebugSystem : IInitializable, IDisposable {
        private const string SHOWN_LABEL = "Haze: on";
        private const string HIDDEN_LABEL = "Haze: off";
        private const string TOGGLE_DESCRIPTION = "Turns the space haze on or off for you only: off, you see the whole field sharp.";
        private const string DISTANCE_LABEL = "Haze clear distance";
        private const string DISTANCE_VALUE_LABEL = "Metres";
        private const string DISTANCE_DESCRIPTION = "How far you see sharp before things fade into the haze (for you only); the fade keeps its length.";

        private readonly SpaceHazeModel _spaceHazeModel;
        private readonly SpaceHazeConfiguration _spaceHazeConfiguration;
        private readonly IDebugActionRegistry _debugActionRegistry;

        private IDisposable _toggleRegistration;
        private IDisposable _distanceRegistration;
        private bool _isRegisteredEnabled;

        public SpaceHazeDebugSystem(SpaceHazeModel spaceHazeModel, SpaceHazeConfiguration spaceHazeConfiguration, IDebugActionRegistry debugActionRegistry) {
            _spaceHazeModel = spaceHazeModel;
            _spaceHazeConfiguration = spaceHazeConfiguration;
            _debugActionRegistry = debugActionRegistry;
        }

        public void Initialize() {
            _spaceHazeModel.OnChanged += OnHazeChanged;
            RegisterToggle();
            DebugActionValue distance = new(DISTANCE_VALUE_LABEL, _spaceHazeConfiguration.ClearMeters);
            _distanceRegistration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.VIEW, DISTANCE_LABEL, DISTANCE_DESCRIPTION, ReadUnavailableReason, distance, SetClearMeters));
        }

        public void Dispose() {
            _spaceHazeModel.OnChanged -= OnHazeChanged;
            _toggleRegistration.Dispose();
            _distanceRegistration.Dispose();
        }

        private void RegisterToggle() {
            _isRegisteredEnabled = _spaceHazeModel.IsEnabled;
            string label = _isRegisteredEnabled ? SHOWN_LABEL : HIDDEN_LABEL;
            _toggleRegistration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.VIEW, label, TOGGLE_DESCRIPTION, ReadUnavailableReason, ToggleHaze));
        }

        private string ReadUnavailableReason() =>
            null;

        private void ToggleHaze() =>
            _spaceHazeModel.SetEnabled(_spaceHazeModel.IsEnabled == false);

        private void SetClearMeters(float clearMeters) =>
            _spaceHazeModel.SetClearMetersOverride(Math.Max(clearMeters, 0f));

        private void OnHazeChanged() {
            if (_spaceHazeModel.IsEnabled == _isRegisteredEnabled)
                return;

            _toggleRegistration.Dispose();
            RegisterToggle();
        }
    }
}
#endif
