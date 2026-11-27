using Features.ConstellationModule.Data;
using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Definitions;
using UnityEngine;
using Zenject;

namespace Features.ConstellationModule.Scripts.Views {
    // Shows this peer's sky as ConstellationRevealModel says (filled from the synced sky): the shown constellation at
    // its reveal time, and the one switched away from fading out as a whole on a figure of its own, so the two can
    // cross-fade.
    public sealed class ConstellationHighlightView : MonoBehaviour {
        [SerializeField] private ConstellationFigureView _shownFigure;
        [SerializeField] private ConstellationFigureView _fadingFigure;

        private ConstellationRevealModel _constellationRevealModel;
        private ConstellationCatalog _constellationCatalog;
        private IConstellationRevealTimelineFactory _constellationRevealTimelineFactory;
        private IConstellationFadeRule _constellationFadeRule;
        private ConstellationRevealTimeline _shownTimeline;
        private ConstellationRevealTimeline _fadingTimeline;

        [Inject]
        private void InjectDependencies(ConstellationRevealModel constellationRevealModel, ConstellationCatalog constellationCatalog, IConstellationRevealTimelineFactory constellationRevealTimelineFactory, IConstellationFadeRule constellationFadeRule) {
            _constellationRevealModel = constellationRevealModel;
            _constellationCatalog = constellationCatalog;
            _constellationRevealTimelineFactory = constellationRevealTimelineFactory;
            _constellationFadeRule = constellationFadeRule;
        }

        private void OnEnable() {
            _constellationRevealModel.OnFiguresChanged += OnFiguresChanged;
            OnFiguresChanged();
        }

        private void OnDisable() =>
            _constellationRevealModel.OnFiguresChanged -= OnFiguresChanged;

        private void LateUpdate() {
            if (_constellationRevealModel.HasShown)
                _shownFigure.Draw(_shownTimeline, _constellationRevealModel.ShownRevealSeconds, 1f);
            if (_constellationRevealModel.HasFading)
                _fadingFigure.Draw(_fadingTimeline, _constellationRevealModel.FadingRevealSeconds, _constellationFadeRule.GetFadeOutAlpha(_constellationRevealModel.FadingSeconds));
        }

        private ConstellationRevealTimeline ShowFigure(ConstellationFigureView figure, int constellationIndex) {
            if (constellationIndex == ConstellationRevealModel.NO_CONSTELLATION) {
                figure.Clear();
                return null;
            }

            ConstellationDefinition constellation = _constellationCatalog.Constellations[constellationIndex];
            figure.Show(constellation);
            return _constellationRevealTimelineFactory.Create(constellation);
        }

        private void OnFiguresChanged() {
            _shownTimeline = ShowFigure(_shownFigure, _constellationRevealModel.ShownConstellation);
            _fadingTimeline = ShowFigure(_fadingFigure, _constellationRevealModel.FadingConstellation);
        }
    }
}
