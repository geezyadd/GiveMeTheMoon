using System.Collections.Generic;
using Features.EmoteModule.Data;
using UnityEngine;

namespace Features.EmoteModule.UI {
    public sealed class EmoteWheelView : EmoteWheelViewBase {
        [SerializeField] private EmoteWheelSector _sectorTemplate;
        [SerializeField] private RectTransform _sectorsRoot;

        private readonly List<EmoteWheelSector> _sectors = new();

        public override void ShowEmotes(IReadOnlyList<EmoteEntry> emotes) {
            _sectorTemplate.gameObject.SetActive(false);
            for (int i = 0; i < emotes.Count; i++) {
                EmoteWheelSector sector = Instantiate(_sectorTemplate, _sectorsRoot);
                sector.gameObject.SetActive(true);
                sector.Bind(i, emotes.Count, emotes[i]);
                _sectors.Add(sector);
            }
        }

        public override void Highlight(int index) {
            for (int i = 0; i < _sectors.Count; i++)
                _sectors[i].SetHighlighted(i == index);
        }
    }
}
