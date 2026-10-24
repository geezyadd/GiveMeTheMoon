using System;
using System.Collections.Generic;
using Features.MvpModule;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using UnityEngine;

namespace Features.LobbyModule.Scripts {
    // Every player's ship panel from the synced models: hull, the installed modules (broken ones red, blinking when
    // they break) and the open breaches; hidden while there is no ship (the lobby).
    public sealed class ShipConditionPresenter : PresenterBehaviour<ShipConditionViewBase> {
        private const string HULL_FORMAT = "Hull {0} / {1}";
        private const string BREACHES_FORMAT = "Breaches: {0}";

        private readonly IReadOnlyShipStatsModel _shipStatsModel;
        private readonly IReadOnlyShipSocketsModel _shipSocketsModel;
        private readonly IReadOnlyShipConditionModel _shipConditionModel;
        private readonly ModuleCatalog _moduleCatalog;
        private readonly ShipHullHudConfiguration _shipHullHudConfiguration;
        private readonly List<string> _socketIds = new List<string>();
        private readonly List<ShipConditionModuleDisplay> _modules = new List<ShipConditionModuleDisplay>();
        private readonly HashSet<string> _shownBroken = new HashSet<string>();

        private bool _isPanelShown;

        public ShipConditionPresenter(
            IReadOnlyShipStatsModel shipStatsModel,
            IReadOnlyShipSocketsModel shipSocketsModel,
            IReadOnlyShipConditionModel shipConditionModel,
            ModuleCatalog moduleCatalog,
            ShipHullHudConfiguration shipHullHudConfiguration) {
            _shipStatsModel = shipStatsModel;
            _shipSocketsModel = shipSocketsModel;
            _shipConditionModel = shipConditionModel;
            _moduleCatalog = moduleCatalog;
            _shipHullHudConfiguration = shipHullHudConfiguration;
        }

        protected override void OnViewSet() {
            _shipStatsModel.OnChanged += Refresh;
            _shipStatsModel.OnAvailableChanged += Refresh;
            _shipSocketsModel.OnStatesChanged += Refresh;
            _shipSocketsModel.OnAvailableChanged += Refresh;
            _shipConditionModel.OnChanged += Refresh;
            _shipConditionModel.OnAvailableChanged += Refresh;
            Refresh();
        }

        protected override void OnDisposed() {
            _shipStatsModel.OnChanged -= Refresh;
            _shipStatsModel.OnAvailableChanged -= Refresh;
            _shipSocketsModel.OnStatesChanged -= Refresh;
            _shipSocketsModel.OnAvailableChanged -= Refresh;
            _shipConditionModel.OnChanged -= Refresh;
            _shipConditionModel.OnAvailableChanged -= Refresh;
        }

        private void Refresh() {
            bool hasShip = _shipStatsModel.IsAvailable && _shipStatsModel.HullMax > 0f;
            View.SetPanelVisible(hasShip);
            if (hasShip == false) {
                _isPanelShown = false;
                _shownBroken.Clear();
                return;
            }

            View.SetHull(string.Format(HULL_FORMAT, Mathf.CeilToInt(_shipStatsModel.Hull), Mathf.CeilToInt(_shipStatsModel.HullMax)));
            int breaches = _shipConditionModel.OpenBreaches.Count;
            View.SetBreaches(string.Format(BREACHES_FORMAT, breaches), breaches > 0);
            CollectModules();
            View.SetModules(_modules, _shipHullHudConfiguration.BrokenBlinkSeconds, _shipHullHudConfiguration.BrokenBlinksPerSecond);
            _isPanelShown = true;
        }

        // Sorted by socket id, so a module keeps its place on the panel whatever order the sync delivers.
        private void CollectModules() {
            _socketIds.Clear();
            foreach (KeyValuePair<string, ShipSocketState> pair in _shipSocketsModel.States) {
                if (pair.Value.Occupied)
                    _socketIds.Add(pair.Key);
            }

            _socketIds.Sort(StringComparer.Ordinal);
            _modules.Clear();
            foreach (string socketId in _socketIds) {
                if (_moduleCatalog.TryGet(_shipSocketsModel.States[socketId].ModuleId, out ModuleDefinition module) == false)
                    continue;

                bool isBroken = IsBroken(socketId);
                // The first state a player sees (a join at a station included) is no fresh break.
                bool hasJustBroken = isBroken && _isPanelShown && _shownBroken.Contains(socketId) == false;
                _modules.Add(new ShipConditionModuleDisplay(module.Icon, module.DisplayName, isBroken, hasJustBroken));
            }

            RememberBroken();
        }

        private void RememberBroken() {
            _shownBroken.Clear();
            foreach (string socketId in _shipConditionModel.BrokenSockets)
                _shownBroken.Add(socketId);
        }

        private bool IsBroken(string socketId) {
            foreach (string brokenSocketId in _shipConditionModel.BrokenSockets) {
                if (string.Equals(brokenSocketId, socketId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
