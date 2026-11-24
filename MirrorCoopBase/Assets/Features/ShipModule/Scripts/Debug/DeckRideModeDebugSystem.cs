#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.DebugModule.Contracts;
using Features.ShipModule.Scripts.Riding;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only: the debug window's "Deck ride: new / old" toggle for this peer's own player. A button label cannot
    // change, so the toggle registers again with the label of the new state. It applies the next time the player gets on
    // the deck.
    public sealed class DeckRideModeDebugSystem : IInitializable, IDisposable {
        private const string NEW_LABEL = "Deck ride: new";
        private const string OLD_LABEL = "Deck ride: old";
        private const string DESCRIPTION = "How your own player stands on the flying ship's deck. New: a physical body the ship carries, synced in the ship's axes. Old: bound to the deck as before. Seats bind in both. Takes effect the next time you get on the deck.";

        private readonly DeckRideModeModel _modeModel;
        private readonly IDebugActionRegistry _debugActionRegistry;

        private IDisposable _registration;
        private bool _isRegisteredNew;

        public DeckRideModeDebugSystem(DeckRideModeModel modeModel, IDebugActionRegistry debugActionRegistry) {
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
            _isRegisteredNew = _modeModel.UsesCarrier;
            string label = _isRegisteredNew ? NEW_LABEL : OLD_LABEL;
            _registration = _debugActionRegistry.Register(
                new DebugActionDefinition(DebugSections.SHIP, label, DESCRIPTION, ReadUnavailableReason, Toggle));
        }

        private string ReadUnavailableReason() =>
            null;

        private void Toggle() =>
            _modeModel.SetUsesCarrier(_modeModel.UsesCarrier == false);

        private void OnModeChanged() {
            if (_modeModel.UsesCarrier == _isRegisteredNew)
                return;

            _registration.Dispose();
            Register();
        }
    }
}
#endif
