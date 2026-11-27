using System.Collections.Generic;
using Features.ConstellationModule.Data;
using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Definitions;
using UnityEngine;
using Zenject;

namespace Features.ConstellationModule.Scripts.Views {
    // Shows this peer's sky as ConstellationRevealModel says (filled from the synced sky): the shown constellation (or
    // every catalog constellation, all at the same reveal time) at its reveal time, and the one switched away from
    // fading out as a whole on a figure of its own, so the two can cross-fade.
    public sealed class ConstellationHighlightView : MonoBehaviour {
        [SerializeField] private ConstellationFigureView _shownFigure;
        [SerializeField] private ConstellationFigureView _fadingFigure;

        private readonly List<ConstellationRevealTimeline> _shownTimelines = new();
        private readonly List<ConstellationRevealTimeline> _fadingTimelines = new();

        private ConstellationRevealModel _constellationRevealModel;
        private ConstellationCatalog _constellationCatalog;
        private IConstellationRevealTimelineFactory _constellationRevealTimelineFactory;
        private IConstellationFadeRule _constellationFadeRule;

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
                _shownFigure.Draw(_shownTimelines, _constellationRevealModel.ShownRevealSeconds, 1f);
            if (_constellationRevealModel.HasFading)
                _fadingFigure.Draw(_fadingTimelines, _constellationRevealModel.FadingRevealSeconds, _constellationFadeRule.GetFadeOutAlpha(_constellationRevealModel.FadingSeconds));
        }

        private void ShowFigure(ConstellationFigureView figure, int constellationIndex, List<ConstellationRevealTimeline> timelines) {
            timelines.Clear();
            if (constellationIndex == ConstellationRevealModel.NO_CONSTELLATION) {
                figure.Clear();
                return;
            }

            IReadOnlyList<ConstellationDefinition> constellations = GetConstellations(constellationIndex);
            figure.Show(constellations);
            foreach (ConstellationDefinition constellation in constellations)
                timelines.Add(_constellationRevealTimelineFactory.Create(constellation));
        }

        private IReadOnlyList<ConstellationDefinition> GetConstellations(int constellationIndex) =>
            constellationIndex == ConstellationRevealModel.ALL_CONSTELLATIONS ? _constellationCatalog.Constellations : new[] { _constellationCatalog.Constellations[constellationIndex] };

        private void OnFiguresChanged() {
            ShowFigure(_shownFigure, _constellationRevealModel.ShownConstellation, _shownTimelines);
            ShowFigure(_fadingFigure, _constellationRevealModel.FadingConstellation, _fadingTimelines);
        }
    }
}
