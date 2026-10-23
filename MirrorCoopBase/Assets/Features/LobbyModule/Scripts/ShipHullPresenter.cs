using Features.MvpModule;
using Features.ShipModule.Scripts.Generated;
using UnityEngine;

namespace Features.LobbyModule.Scripts {
    // Every player's hull bar from the synced ship stats; hidden while there is no ship (the lobby).
    public sealed class ShipHullPresenter : PresenterBehaviour<ShipHullViewBase> {
        private const string HULL_FORMAT = "hull {0} / {1}";

        private readonly IReadOnlyShipStatsModel _shipStatsModel;

        private bool _isHullShown;
        private float _shownHull;

        public ShipHullPresenter(IReadOnlyShipStatsModel shipStatsModel) =>
            _shipStatsModel = shipStatsModel;

        protected override void OnViewSet() {
            _shipStatsModel.OnHullChanged += Refresh;
            _shipStatsModel.OnHullMaxChanged += Refresh;
            _shipStatsModel.OnAvailableChanged += Refresh;
            Refresh();
        }

        protected override void OnDisposed() {
            _shipStatsModel.OnHullChanged -= Refresh;
            _shipStatsModel.OnHullMaxChanged -= Refresh;
            _shipStatsModel.OnAvailableChanged -= Refresh;
        }

        private void Refresh() {
            // The bridge clears the model to zeros when the ship goes away: no max, no bar and no flash.
            bool hasHull = _shipStatsModel.IsAvailable && _shipStatsModel.HullMax > 0f;
            View.SetHullVisible(hasHull);
            if (hasHull == false) {
                _isHullShown = false;
                return;
            }

            float hull = _shipStatsModel.Hull;
            float hullMax = _shipStatsModel.HullMax;
            // The first value a player sees (a join at a station included) is no hit.
            if (_isHullShown && hull < _shownHull)
                View.PlayDamageFlash();

            _isHullShown = true;
            _shownHull = hull;
            View.SetHull(hull / hullMax, string.Format(HULL_FORMAT, Mathf.CeilToInt(hull), Mathf.CeilToInt(hullMax)));
        }
    }
}
