#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Features.MenuModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Game.Connection;
using Mirror;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // The connection tests' second (or third) player, in a development build: takes the host's commands one by one
    // (CoopConnectionCommands: join, leave, sit, die, wait for a phase, walk, host, ...), runs each through the menu, the
    // HUD or the player's real commands and answers it with a report of what this process sees.
    public sealed class CoopConnectionClient : MonoBehaviour {
        private const float SETTLE_SECONDS = 1.5f;
        private const int SCREENSHOT_FRAMES = 3;

        private readonly WaitForSeconds _settle = new WaitForSeconds(SETTLE_SECONDS);
        private readonly WaitForSeconds _exchangePoll = new WaitForSeconds(CoopTestExchange.POLL_SECONDS);

        private CoopClientSession _session;
        private CoopClientPlayerActions _player;
        private Coroutine _commands;
        private int _takenCommands;
        private string _playerName = CoopTestPlayers.CLIENT_NAME;

        public void Run(CoopTestArguments arguments) {
            _session = new CoopClientSession(arguments);
            _player = new CoopClientPlayerActions(_session);
            // The client window is not focused while the editor hosts the test.
            Application.runInBackground = true;
            _commands = StartCoroutine(RunCommandsCoroutine());
        }

        private void OnDestroy() {
            if (_commands != null)
                StopCoroutine(_commands);

            _commands = null;
            _session.Dispose();
        }

        private IEnumerator RunCommandsCoroutine() {
            while (true) {
                List<string> commands = _session.Exchange.ReadCommandsFrom(_takenCommands);
                if (commands.Count == 0) {
                    yield return _exchangePoll;
                    continue;
                }

                _takenCommands++;
                yield return RunCommandCoroutine(commands[0]);
            }
        }

        private IEnumerator RunCommandCoroutine(string line) {
            int separator = line.IndexOf(CoopConnectionCommands.ARGUMENT_SEPARATOR);
            string command = separator < 0 ? line : line.Substring(0, separator);
            string argument = separator < 0 ? string.Empty : line.Substring(separator + 1);
            switch (command) {
                case CoopConnectionCommands.JOIN:
                    yield return JoinCoroutine(argument);
                    break;
                case CoopConnectionCommands.JOIN_EXPECT_REJECT:
                    yield return JoinExpectingRejectCoroutine(argument);
                    yield break;
                case CoopConnectionCommands.HOST_AND_LEAVE:
                    yield return HostAndLeaveCoroutine();
                    yield break;
                case CoopConnectionCommands.SCREENSHOT:
                    yield return ScreenshotCoroutine(argument);
                    break;
                case CoopConnectionCommands.QUIT:
                    _session.Report(_session.Capture(command));
                    Application.Quit();
                    yield break;
                default:
                    yield return _player.RunCoroutine(command, argument);
                    break;
            }

            CoopObservation observation = _session.Capture(command);
            observation.RunState.WalkedMeters = _player.WalkedMeters;
            _session.Report(observation);
        }

        private IEnumerator JoinCoroutine(string playerName) {
            yield return _session.WaitForCoroutine(SessionQueries.IsMenuReady, "the menu");
            JoinThroughMenu(playerName);
            yield return _session.WaitForCoroutine(IsInLobbyOrAtStation, "the host's lobby or game at a station");
            yield return _settle;
        }

        private IEnumerator JoinExpectingRejectCoroutine(string playerName) {
            yield return _session.WaitForCoroutine(SessionQueries.IsMenuReady, "the menu");
            JoinThroughMenu(playerName);
            yield return _session.WaitForCoroutine(CoopSessionActions.IsRejectedInMenu, "the answer to a join that must be turned away");
            ConnectionRejectReason reason = SessionQueries.Resolve<ConnectionRejectModel>().LastRejection;
            CoopObservation observation = _session.Capture(CoopConnectionCommands.JOIN_EXPECT_REJECT);
            observation.RejectReason = reason.ToString();
            observation.IsRejectTextShown = CoopSessionActions.IsRejectTextShown(reason);
            _session.Report(observation);
        }

        // Hosts a session of its own on the test's port once the editor host is gone, reports it from the lobby, and
        // leaves it again: what a player does after the host quit.
        private IEnumerator HostAndLeaveCoroutine() {
            yield return _session.WaitForCoroutine(SessionQueries.IsMenuReady, "the menu with a bound Host button");
            CoopSessionActions.PrepareDirect(_session.Arguments.Port, _playerName);
            Object.FindAnyObjectByType<MenuSessionViewBase>().OnHostClicked.Invoke();
            yield return _session.WaitForCoroutine(IsHostInOwnLobby, "the own lobby as the host");
            yield return _settle;
            CoopObservation hosting = _session.Capture(CoopConnectionCommands.HOST_AND_LEAVE);
            CoopSessionActions.LeaveThroughHud();
            yield return _session.WaitForCoroutine(SessionQueries.IsBackInMenu, "the menu after leaving the own session");
            hosting.Errors = _session.Capture(CoopConnectionCommands.HOST_AND_LEAVE).Errors;
            _session.Report(hosting);
        }

        private IEnumerator ScreenshotCoroutine(string name) {
            string path = _session.ScreenshotPath(name);
            ScreenCapture.CaptureScreenshot(path);
            for (int frame = 0; frame < SCREENSHOT_FRAMES; frame++)
                yield return null;

            yield return _session.WaitForCoroutine(() => File.Exists(path), $"the screenshot {path}");
        }

        private void JoinThroughMenu(string playerName) {
            if (string.IsNullOrEmpty(playerName) == false)
                _playerName = playerName;

            CoopSessionActions.PrepareDirect(_session.Arguments.Port, _playerName);
            CoopSessionActions.JoinThroughMenu(_session.Arguments.Address);
        }

        private static bool IsHostInOwnLobby() {
            bool isHostClientReady = NetworkServer.active && NetworkClient.ready && NetworkClient.connection.isAuthenticated;
            ConnectionNetworkManager manager = SessionQueries.NetworkManager;
            bool isLobbyActive = SceneManager.GetActiveScene().name == manager.LobbySceneName && manager.IsMapLoaded;
            return isHostClientReady && isLobbyActive && SessionQueries.LocalPlayer != null;
        }

        private static bool IsInLobbyOrAtStation() =>
            CoopSessionActions.IsClientInLobby() || (SessionQueries.IsGameAtStation() && SessionQueries.Resolve<IReadOnlyShipRunModel>().IsAvailable);
    }
}
#endif
