using System;
using Features.ConstellationModule.Data;
using Features.ConstellationModule.Data.Generated;
using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Definitions;
using Mirror;
using Zenject;

namespace Features.ConstellationModule.Scripts.Systems {
    // Runs this peer's sky from the synced lobby sky: a pick or a hide on the server reaches the reveal model, the reveal
    // clock is the network time, so every peer, late joiners too, sees the same reveal at the same moment. Drops the old
    // figure once its fade-out is over; leaving the lobby clears the sky.
    public sealed class ConstellationRevealSystem : IInitializable, IDisposable, ITickable {
        private const string UNKNOWN_CONSTELLATION_FORMAT = "The synced lobby sky shows '{0}', which is not in this peer's constellation catalog.";

        private readonly IReadOnlyConstellationSkyModel _constellationSkyModel;
        private readonly ConstellationRevealModel _constellationRevealModel;
        private readonly ConstellationCatalog _constellationCatalog;
        private readonly IConstellationSkyStateRule _constellationSkyStateRule;
        private readonly IConstellationFadeRule _constellationFadeRule;

        public ConstellationRevealSystem(IReadOnlyConstellationSkyModel constellationSkyModel, ConstellationRevealModel constellationRevealModel, ConstellationCatalog constellationCatalog, IConstellationSkyStateRule constellationSkyStateRule, IConstellationFadeRule constellationFadeRule) {
            _constellationSkyModel = constellationSkyModel;
            _constellationRevealModel = constellationRevealModel;
            _constellationCatalog = constellationCatalog;
            _constellationSkyStateRule = constellationSkyStateRule;
            _constellationFadeRule = constellationFadeRule;
        }

        public void Initialize() {
            _constellationSkyModel.OnChanged += OnSkyChanged;
            OnSkyChanged();
        }

        public void Dispose() {
            _constellationSkyModel.OnChanged -= OnSkyChanged;
            _constellationRevealModel.Clear();
        }

        public void Tick() {
            _constellationRevealModel.SetTime(NetworkTime.time);
            if (_constellationRevealModel.HasFading && _constellationFadeRule.IsFadeOutOver(_constellationRevealModel.FadingSeconds))
                _constellationRevealModel.EndFading();
        }

        private void OnSkyChanged() {
            if (_constellationSkyModel.IsAvailable == false) {
                _constellationRevealModel.Clear();
                return;
            }

            ConstellationSkyState state = ReadSkyState();
            if (_constellationSkyStateRule.IsShown(state) == false) {
                _constellationRevealModel.Hide(state.ChangeTime);
                return;
            }

            if (_constellationCatalog.TryFindIndex(state.ConstellationId, out int constellation) == false)
                throw new InvalidOperationException(string.Format(UNKNOWN_CONSTELLATION_FORMAT, state.ConstellationId));

            _constellationRevealModel.Show(constellation, state.RevealStartTime, state.ChangeTime);
        }

        private ConstellationSkyState ReadSkyState() =>
            new() {
                ConstellationId = _constellationSkyModel.ConstellationId,
                RevealStartTime = _constellationSkyModel.RevealStartTime,
                ChangeTime = _constellationSkyModel.ChangeTime,
            };
    }
}
