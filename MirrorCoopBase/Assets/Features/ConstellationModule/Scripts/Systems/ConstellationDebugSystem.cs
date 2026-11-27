#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.ConstellationModule.Data;
using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Definitions;
using Features.DebugModule.Contracts;
using Mirror;
using Zenject;

namespace Features.ConstellationModule.Scripts.Systems {
    // Debug only: the debug window's "Constellations" section, one button per catalog constellation and "Hide", for the
    // whole crew's sky. Registered on the host only (the server writes the sky); a pure client gets no section.
    public sealed class ConstellationDebugSystem : IInitializable, IDisposable {
        private const string SHOW_DESCRIPTION_FORMAT = "Reveals {0} on every player's sky: its stars light up and its lines are drawn one after another. Picked while shown, it fades out and plays again.";
        private const string HIDE_LABEL = "Hide";
        private const string HIDE_DESCRIPTION = "Fades the shown constellation out on every player's sky.";
        private const string NOTHING_SHOWN_REASON = "No constellation is shown.";

        private readonly ConstellationRevealModel _constellationRevealModel;
        private readonly ConstellationCatalog _constellationCatalog;
        private readonly IConstellationService _constellationService;
        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly List<IDisposable> _registrations = new();

        public ConstellationDebugSystem(ConstellationRevealModel constellationRevealModel, ConstellationCatalog constellationCatalog, IConstellationService constellationService, IDebugActionRegistry debugActionRegistry) {
            _constellationRevealModel = constellationRevealModel;
            _constellationCatalog = constellationCatalog;
            _constellationService = constellationService;
            _debugActionRegistry = debugActionRegistry;
        }

        // The lobby is loaded after the host has started its server, so a peer that is not the server here never
        // becomes one while this lobby lives.
        public void Initialize() {
            if (NetworkServer.active == false)
                return;

            foreach (ConstellationDefinition constellation in _constellationCatalog.Constellations)
                RegisterShow(constellation);
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.CONSTELLATIONS, HIDE_LABEL, HIDE_DESCRIPTION, ReadHideUnavailableReason, _constellationService.ServerHide)));
        }

        public void Dispose() {
            foreach (IDisposable registration in _registrations)
                registration.Dispose();
            _registrations.Clear();
        }

        private void RegisterShow(ConstellationDefinition constellation) {
            string description = string.Format(SHOW_DESCRIPTION_FORMAT, constellation.DisplayName);
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.CONSTELLATIONS, constellation.DisplayName, description, ReadShowUnavailableReason, () => _constellationService.ServerShow(constellation.Id))));
        }

        private string ReadShowUnavailableReason() =>
            null;

        private string ReadHideUnavailableReason() =>
            _constellationRevealModel.HasShown ? null : NOTHING_SHOWN_REASON;
    }
}
#endif
