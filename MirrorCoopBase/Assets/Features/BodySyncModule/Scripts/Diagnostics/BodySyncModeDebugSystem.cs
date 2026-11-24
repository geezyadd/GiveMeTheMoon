#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.BodySyncModule.Models;
using Features.DebugModule.Contracts;
using Zenject;

namespace Features.BodySyncModule.Diagnostics {
    // Debug only: the debug window's "Player sync: old / new" toggle for how this peer shows the other players. A button
    // label cannot change, so the toggle registers again with the label of the new state.
    public sealed class BodySyncModeDebugSystem : IInitializable, IDisposable {
        private const string OLD_LABEL = "Player sync: old";
        private const string NEW_LABEL = "Player sync: new";
        private const string DESCRIPTION = "How this game shows the other players. Old: the network body replays their positions. New: each one is a simulated copy that walks and jumps from its owner's controls and is pulled toward the owner's state packets. Only this game changes; the others keep their own choice.";

        private readonly BodySyncModeModel _modeModel;
        private readonly IDebugActionRegistry _debugActionRegistry;

        private IDisposable _registration;
        private bool _isRegisteredNew;

        public BodySyncModeDebugSystem(BodySyncModeModel modeModel, IDebugActionRegistry debugActionRegistry) {
            _modeModel = modeModel;
            _debugActionRegistry = debugActionRegistry;
        }

        public void Initialize() {
            _modeModel.OnChanged += OnModeChanged;
            Register();
        }

        public void Dispose() {
            _modeModel.OnChanged -= OnModeChanged;
            _registration?.Dispose();
        }

        private void Register() {
            _isRegisteredNew = _modeModel.SimulatesCopies;
            string label = _isRegisteredNew ? NEW_LABEL : OLD_LABEL;
            _registration = _debugActionRegistry.Register(
                new DebugActionDefinition(DebugSections.NETWORK, label, DESCRIPTION, ReadUnavailableReason, Toggle));
        }

        private string ReadUnavailableReason() =>
            null;

        private void Toggle() =>
            _modeModel.SetSimulatesCopies(_modeModel.SimulatesCopies == false);

        private void OnModeChanged() {
            if (_modeModel.SimulatesCopies == _isRegisteredNew)
                return;

            _registration.Dispose();
            Register();
        }
    }
}
#endif
