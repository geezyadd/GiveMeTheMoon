using System;
using Features.ConstellationModule.Data;
using Features.ConstellationModule.Scripts.Core;
using UnityEngine;
using Zenject;

namespace Features.ConstellationModule.Scripts.Systems {
    // Runs the reveal clock of this peer's sky and drops the old figure once its fade-out is over. The sky is lobby only:
    // leaving the lobby clears it, so coming back shows nothing until a constellation is picked again.
    public sealed class ConstellationRevealSystem : ITickable, IDisposable {
        private readonly ConstellationRevealModel _constellationRevealModel;
        private readonly IConstellationFadeRule _constellationFadeRule;

        public ConstellationRevealSystem(ConstellationRevealModel constellationRevealModel, IConstellationFadeRule constellationFadeRule) {
            _constellationRevealModel = constellationRevealModel;
            _constellationFadeRule = constellationFadeRule;
        }

        public void Dispose() =>
            _constellationRevealModel.Clear();

        public void Tick() {
            _constellationRevealModel.Advance(Time.deltaTime);
            if (_constellationRevealModel.HasFading && _constellationFadeRule.IsFadeOutOver(_constellationRevealModel.FadingSeconds))
                _constellationRevealModel.EndFading();
        }
    }
}
