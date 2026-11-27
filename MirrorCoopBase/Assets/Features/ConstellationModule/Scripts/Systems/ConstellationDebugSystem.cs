#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.ConstellationModule.Data;
using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Definitions;
using Features.DebugModule.Contracts;
using Zenject;

namespace Features.ConstellationModule.Scripts.Systems {
    // Debug only: the debug window's "Constellations" section, one button per catalog constellation and "Hide", for
    // this peer's own sky.
    public sealed class ConstellationDebugSystem : IInitializable, IDisposable {
        private const string SHOW_DESCRIPTION_FORMAT = "Reveals {0} on your sky (for you only): its stars light up and its lines are drawn one after another. Picked while shown, it fades out and plays again.";
        private const string HIDE_LABEL = "Hide";
        private const string HIDE_DESCRIPTION = "Fades the shown constellation out (for you only).";
        private const string NOTHING_SHOWN_REASON = "No constellation is shown.";

        private readonly ConstellationRevealModel _constellationRevealModel;
        private readonly ConstellationCatalog _constellationCatalog;
        private readonly IConstellationFadeRule _constellationFadeRule;
        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly List<IDisposable> _registrations = new();

        public ConstellationDebugSystem(ConstellationRevealModel constellationRevealModel, ConstellationCatalog constellationCatalog, IConstellationFadeRule constellationFadeRule, IDebugActionRegistry debugActionRegistry) {
            _constellationRevealModel = constellationRevealModel;
            _constellationCatalog = constellationCatalog;
            _constellationFadeRule = constellationFadeRule;
            _debugActionRegistry = debugActionRegistry;
        }

        public void Initialize() {
            for (int i = 0; i < _constellationCatalog.Constellations.Count; i++)
                RegisterShow(i);
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.CONSTELLATIONS, HIDE_LABEL, HIDE_DESCRIPTION, ReadHideUnavailableReason, Hide)));
        }

        public void Dispose() {
            foreach (IDisposable registration in _registrations)
                registration.Dispose();
            _registrations.Clear();
        }

        private void RegisterShow(int constellation) {
            string displayName = _constellationCatalog.Constellations[constellation].DisplayName;
            string description = string.Format(SHOW_DESCRIPTION_FORMAT, displayName);
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.CONSTELLATIONS, displayName, description, ReadShowUnavailableReason, () => Show(constellation))));
        }

        private string ReadShowUnavailableReason() =>
            null;

        private string ReadHideUnavailableReason() =>
            _constellationRevealModel.HasShown ? null : NOTHING_SHOWN_REASON;

        private void Show(int constellation) {
            bool isReplay = _constellationRevealModel.ShownConstellation == constellation;
            float revealDelaySeconds = _constellationFadeRule.GetRevealStartDelaySeconds(_constellationRevealModel.HasShown, isReplay);
            _constellationRevealModel.Show(constellation, revealDelaySeconds);
        }

        private void Hide() =>
            _constellationRevealModel.Hide();
    }
}
#endif
