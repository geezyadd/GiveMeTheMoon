using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;
using UnityEngine;

namespace Features.ShopModule.Scripts.UI {
    // The shop window's "Crew" section: one row per dead crewmate who can be bought back.
    public sealed class CrewReviveView : CrewReviveViewBase {
        [SerializeField] private CrewReviveRow _rowTemplate;
        [SerializeField] private RectTransform _rowParent;

        private readonly List<CrewReviveRow> _rows = new();

        public override void DisposeView() {
            foreach (CrewReviveRow row in _rows)
                row.OnReviveClicked -= OnRowReviveClicked;

            base.DisposeView();
        }

        public override void SetCrew(IReadOnlyList<CrewReviveDisplay> crew) {
            _rowTemplate.gameObject.SetActive(false);
            while (_rows.Count < crew.Count)
                _rows.Add(CreateRow());

            for (int i = 0; i < _rows.Count; i++) {
                bool isUsed = i < crew.Count;
                _rows[i].gameObject.SetActive(isUsed);
                if (isUsed)
                    _rows[i].Bind(crew[i]);
            }
        }

        private CrewReviveRow CreateRow() {
            CrewReviveRow row = Instantiate(_rowTemplate, _rowParent);
            row.OnReviveClicked += OnRowReviveClicked;
            return row;
        }

        private void OnRowReviveClicked(PlayerKey player) =>
            InvokeReviveClicked(player);
    }
}
