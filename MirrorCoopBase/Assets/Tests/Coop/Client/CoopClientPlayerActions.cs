#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.PlayerLifeModule.Scripts.Spectator;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.TurretModule.Scripts;
using Game.Connection;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // The connection client's own player steps in a session: board, sit at the helm or the turret, die, leave through
    // the HUD, wait for a run phase, the lobby, the menu or a station, walk under the move input. The caller reports.
    public sealed class CoopClientPlayerActions {
        private const float SETTLE_SECONDS = 1.5f;
        private const float SEAT_DROP_HEIGHT = 0.5f;
        private const float WALK_SECONDS = 0.4f;

        private readonly CoopClientSession _session;
        private readonly WaitForSeconds _settle = new WaitForSeconds(SETTLE_SECONDS);
        private readonly WaitForSeconds _walk = new WaitForSeconds(WALK_SECONDS);

        private static CharacterMovableBase Body =>
            SessionQueries.LocalPlayer.GetComponent<CharacterMovableBase>();

        public float WalkedMeters { get; private set; }

        public CoopClientPlayerActions(CoopClientSession session) =>
            _session = session;

        public IEnumerator RunCoroutine(string command, string argument) {
            WalkedMeters = 0f;
            switch (command) {
                case CoopConnectionCommands.BOARD:
                    ShipDeckBoarding.PutAboveStern(Body, FindShip());
                    yield return _settle;
                    break;
                case CoopConnectionCommands.SIT_AT_HELM:
                    yield return SitAtHelmCoroutine();
                    break;
                case CoopConnectionCommands.SIT_AT_TURRET:
                    yield return SitAtTurretCoroutine();
                    break;
                case CoopConnectionCommands.DIE:
                    SessionQueries.LocalPlayer.GetComponent<PlayerLifeBody>().CmdDebugKill();
                    yield return _session.WaitForCoroutine(IsDeadAndSpectating, "the local player dead and spectating");
                    break;
                case CoopConnectionCommands.LEAVE:
                    CoopSessionActions.LeaveThroughHud();
                    yield return _session.WaitForCoroutine(SessionQueries.IsBackInMenu, "the menu after Leave");
                    break;
                case CoopConnectionCommands.WAIT_PHASE:
                    yield return _session.WaitForCoroutine(() => IsInPhase(argument), $"the run phase {argument}");
                    break;
                case CoopConnectionCommands.WAIT_MENU:
                    yield return _session.WaitForCoroutine(SessionQueries.IsBackInMenu, "the menu");
                    yield return _settle;
                    break;
                case CoopConnectionCommands.WAIT_LOBBY:
                    yield return _session.WaitForCoroutine(IsInLobbyWithPlayer, "the lobby with the local player");
                    yield return _settle;
                    break;
                case CoopConnectionCommands.WAIT_STATION:
                    yield return _session.WaitForCoroutine(SessionQueries.IsGameAtStation, "the game at a station");
                    yield return _settle;
                    break;
                case CoopConnectionCommands.WALK:
                    yield return WalkCoroutine();
                    break;
                case CoopConnectionCommands.REPORT:
                    break;
                default:
                    yield return _session.FailCoroutine($"Unknown command '{command}'.");
                    break;
            }
        }

        // Put beside the seat, then the seat's use command, what E sends.
        private IEnumerator SitAtHelmCoroutine() {
            ShipBase ship = FindShip();
            ShipSocket helm = CoopHelm.FindSocket(ship);
            yield return SitCoroutine(ship, helm, ship.transform.InverseTransformPoint(helm.Seat.SitPoint.position));
            yield return _session.WaitForCoroutine(() => CoopHelm.FindStation(ship).IsSteeredByLocalPlayer, "the local player at the helm");
        }

        private IEnumerator SitAtTurretCoroutine() {
            ShipBase ship = FindShip();
            yield return SitCoroutine(ship, CoopTurret.FindSocket(ship), CoopTurret.GunnerSpot);
            yield return _session.WaitForCoroutine(() => CoopTurret.TryFindStation(ship, out ShipTurretStation station) && station.IsOccupiedByLocalPlayer, "the local player at the turret");
        }

        private IEnumerator SitCoroutine(ShipBase ship, ShipSocket socket, Vector3 shipLocalSpot) {
            ShipDeckBoarding.PutAbove(Body, ship, shipLocalSpot, SEAT_DROP_HEIGHT);
            yield return _settle;
            SessionQueries.LocalPlayer.GetComponent<UseController>().RequestUse(socket.GetComponentInChildren<ShipSitInteractable>());
        }

        // Through the same input buffer the keyboard fills; the distance walked on the ground plane.
        private IEnumerator WalkCoroutine() {
            yield return _settle;
            CharacterInputBuffer input = SessionQueries.Resolve<CharacterInputBuffer>();
            Vector3 start = SessionQueries.LocalPlayer.transform.position;
            input.BeginScripted();
            input.SetScripted(Vector2.up, false, false);
            yield return _walk;
            input.EndScripted();
            Vector3 walked = SessionQueries.LocalPlayer.transform.position - start;
            walked.y = 0f;
            WalkedMeters = walked.magnitude;
        }

        private static bool IsInPhase(string phase) =>
            SessionQueries.LocalPlayer != null && FindShip() != null && SessionQueries.Resolve<IReadOnlyShipRunModel>().Phase.ToString() == phase;

        private static bool IsDeadAndSpectating() =>
            SessionQueries.Resolve<IPlayerLifeQuery>().LocalState == PlayerLifeState.Dead && SessionQueries.Resolve<SpectatorModel>().IsSpectating;

        private static bool IsInLobbyWithPlayer() {
            ConnectionNetworkManager manager = SessionQueries.NetworkManager;
            if (manager.IsMapLoaded == false || manager.IsChangingMap)
                return false;

            return SceneManager.GetActiveScene().name == manager.LobbySceneName && FindShip() == null && SessionQueries.LocalPlayer != null;
        }

        private static ShipBase FindShip() =>
            Object.FindAnyObjectByType<ShipBase>();
    }
}
#endif
