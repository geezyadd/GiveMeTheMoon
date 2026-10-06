#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.ShipModule.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Dev-only readout of the synced ship stats, to compare on host and client what the server published.
    public sealed class ShipStatsDebugOverlay : MonoBehaviour {
        private const float LEFT = 10f;
        private const float BOTTOM_OFFSET = 34f;
        private const float WIDTH = 620f;
        private const float HEIGHT = 24f;
        private const string HOST_ROLE = "host";
        private const string CLIENT_ROLE = "client";
        private const string TEXT_FORMAT =
            "Ship stats ({0}): FlightSpeed {1:0.###}  Thrust {2:0.###}  Handling {3:0.###}  Armor {4:0.###}  DodgeRange {5:0.###}";

        private IReadOnlyShipStatsModel _shipStatsModel;
        private string _text;

        [Inject]
        private void InjectDependencies(IReadOnlyShipStatsModel shipStatsModel) =>
            _shipStatsModel = shipStatsModel;

        private void OnEnable() {
            _shipStatsModel.OnChanged += HandleStatsChanged;
            _shipStatsModel.OnAvailableChanged += HandleStatsChanged;
            HandleStatsChanged();
        }

        private void OnDisable() {
            _shipStatsModel.OnChanged -= HandleStatsChanged;
            _shipStatsModel.OnAvailableChanged -= HandleStatsChanged;
        }

        private void OnGUI() {
            if (_shipStatsModel.IsAvailable == false)
                return;

            GUI.Label(new Rect(LEFT, Screen.height - BOTTOM_OFFSET, WIDTH, HEIGHT), _text, GUI.skin.box);
        }

        private void HandleStatsChanged() =>
            _text = string.Format(
                TEXT_FORMAT,
                NetworkServer.active ? HOST_ROLE : CLIENT_ROLE,
                _shipStatsModel.FlightSpeed,
                _shipStatsModel.Thrust,
                _shipStatsModel.Handling,
                _shipStatsModel.Armor,
                _shipStatsModel.DodgeRange);
    }
}
#endif
