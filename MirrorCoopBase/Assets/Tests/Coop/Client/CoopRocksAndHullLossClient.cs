#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using Features.CameraModule.Scripts.Shake;
using Features.ShipImpactModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Game.Connection;
using Mirror;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests.Coop.Client {
    // The client rides through the host's rock hits (scenario rocks-and-hull-loss): it records every rock impact this
    // peer gets and whether its view shook, through the host's small and large rock hits and then a near pass, and
    // reports them with the hull and its damage; the host then destroys the hull in that cruise and the client reports
    // whether it stands free in the lobby. Each step on the host's command.
    public sealed class CoopRocksAndHullLossClient : CoopScenarioClientBot {
        private const int LOOP = 1;
        private const string ROCK_IMPACT_SCREENSHOT = "rock-impact";
        private const float LOBBY_SETTLE_SECONDS = 2f;

        private readonly WaitForSeconds _lobbySettle = new WaitForSeconds(LOBBY_SETTLE_SECONDS);
        private readonly List<string> _impactKinds = new List<string>();
        private IShipImpactEvents _shipImpactEvents;
        private CameraShakeModel _cameraShakeModel;
        private int _unshakenImpacts;
        private int _shakenImpacts;
        private bool _hasCapturedImpact;
        private string _screenshot;

        private static NetworkIdentity Player => SessionQueries.LocalPlayer;

        protected override IEnumerator RunScenarioCoroutine() {
            yield return BoardCoroutine();
            yield return WaitForCruiseCoroutine(LOOP);
            _shipImpactEvents = SessionQueries.Resolve<IShipImpactEvents>();
            _cameraShakeModel = SessionQueries.Resolve<CameraShakeModel>();
            yield return WatchImpactsCoroutine(CoopFlightSteps.COMMAND_WATCH_IMPACTS, CoopFlightSteps.COMMAND_REPORT_IMPACTS);
            yield return WatchImpactsCoroutine(CoopFlightSteps.COMMAND_WATCH_NEAR_PASS, CoopFlightSteps.COMMAND_REPORT_NEAR_PASS);
            yield return ReportFreeInLobbyCoroutine();
        }

        private void OnDisable() =>
            StopWatching();

        // A shake is added by the impact's own handler: it shows in the shake model from the next frame on.
        private void Update() {
            if (_unshakenImpacts == 0 || HasActiveShake() == false)
                return;

            _shakenImpacts += _unshakenImpacts;
            _unshakenImpacts = 0;
        }

        private IEnumerator WatchImpactsCoroutine(string watchCommand, string reportCommand) {
            yield return WaitForCommandCoroutine(watchCommand);
            _impactKinds.Clear();
            _unshakenImpacts = 0;
            _shakenImpacts = 0;
            _screenshot = null;
            _shipImpactEvents.OnImpact += OnImpact;
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.WATCHING));

            yield return WaitForCommandCoroutine(reportCommand);
            StopWatching();
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.IMPACTS);
            observation.ImpactKinds = _impactKinds.ToArray();
            observation.ShakenImpacts = _shakenImpacts;
            observation.IsRiding = Player.GetComponent<ShipRider>().IsRiding;
            observation.Screenshot = _screenshot;
            Report(observation);
        }

        private IEnumerator ReportFreeInLobbyCoroutine() {
            yield return WaitForCoroutine(IsBackInLobby, "the lobby after the hull loss");
            yield return _lobbySettle;
            CoopObservation observation = CoopObserver.CaptureStep(CoopFlightSteps.FREE_IN_LOBBY);
            ShipRider rider = Player.GetComponent<ShipRider>();
            observation.IsRiding = rider.IsRiding;
            observation.IsSeated = rider.IsSeated;
            observation.ReleaseProblems = PlayerReleaseProbe.DescribeProblems(Player);
            yield return PlayerReleaseProbe.WalkCoroutine(Player, Vector2.up, walked => observation.WalkedMeters = walked);
            Report(observation);
        }

        private void StopWatching() {
            if (_shipImpactEvents != null)
                _shipImpactEvents.OnImpact -= OnImpact;
        }

        private void OnImpact(ShipRockImpact impact) {
            _impactKinds.Add(impact.Kind.ToString());
            _unshakenImpacts++;
            if (impact.Kind != ShipRockImpactKind.Large || _hasCapturedImpact)
                return;

            _hasCapturedImpact = true;
            StartCoroutine(CaptureScreenCoroutine(ROCK_IMPACT_SCREENSHOT, path => _screenshot = path));
        }

        private bool HasActiveShake() {
            IReadOnlyList<CameraShakeOneShot> shakes = _cameraShakeModel.OneShots;
            for (int i = 0; i < shakes.Count; i++) {
                if (shakes[i].CurrentStrength > 0f)
                    return true;
            }

            return false;
        }

        private static bool IsBackInLobby() {
            ConnectionNetworkManager manager = SessionQueries.NetworkManager;
            if (manager.IsMapLoaded == false || manager.IsChangingMap)
                return false;

            Scene game = SceneManager.GetSceneByName(manager.GameSceneName);
            bool isGameUnloaded = game.IsValid() == false || game.isLoaded == false;
            return isGameUnloaded && CoopSessionActions.IsClientInLobby();
        }
    }
}
#endif
