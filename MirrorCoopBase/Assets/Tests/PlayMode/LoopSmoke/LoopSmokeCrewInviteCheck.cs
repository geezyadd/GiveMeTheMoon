#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using Features.CameraModule.Scripts;
using Features.CrewInviteModule.Data.Generated;
using Features.CrewInviteModule.Scripts.Core;
using Features.CrewInviteModule.Scripts.Network;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.TooltipModule.Scripts.UI;
using Game.Connection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // The "Hire crew" terminal the way a player meets it: aims at it, reads the tooltip and presses interact. The test
    // session has no Steam lobby, so for the local player the terminal stays inactive with the Steam reason; the shared
    // CrewRoster must count the crew up to the slot limit and back, and close the join window in flight.
    public sealed class LoopSmokeCrewInviteCheck {
        private const int FIRST_FILLER_CONNECTION_ID = 90011;
        private const string FILLER_CLIENT_ID = "loop-smoke-crew-filler-";
        private const string FILLER_NAME = "CrewFiller";
        private const string NO_STEAM_TEXT = "Steam is not running.";
        private const float APPROACH_DISTANCE = 2.5f;
        private const float STAND_HEIGHT = 0.8f;
        private const float STAND_SETTLE_SECONDS = 1f;
        private const int FRAMES_FOR_INTERACT_ROUND_TRIP = 10;

        private readonly IReadOnlyCrewRosterModel _crewRosterModel = LoopSmokeShopCheck.Resolve<IReadOnlyCrewRosterModel>();
        private readonly ICrewInviteStatusService _crewInviteStatusService = LoopSmokeShopCheck.Resolve<ICrewInviteStatusService>();
        private readonly int _playerSlots = LoopSmokeShopCheck.Resolve<ConnectionConfig>().MaxConnections;
        private readonly LoopSmokeSteamLobbySpy _steamLobby;

        public LoopSmokeCrewInviteCheck(LoopSmokeSteamLobbySpy steamLobby) =>
            _steamLobby = steamLobby;

        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static LocalPlayerInteraction Interaction => Player.GetComponent<LocalPlayerInteraction>();

        public IEnumerator CheckInLobbyCoroutine() {
            CrewInviteInteractable[] terminals = Object.FindObjectsByType<CrewInviteInteractable>(FindObjectsSortMode.None);
            Assert.AreEqual(1, terminals.Length, "The lobby does not have exactly one crew invite terminal.");
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => IsRosterAt(1), "the crew roster with the host alone");
            Assert.AreEqual(_playerSlots, _crewRosterModel.PlayerSlots, "The crew roster does not show the player slots.");
            Assert.IsTrue(_crewRosterModel.IsJoinWindowOpen, "The crew roster reports the lobby closed to new players.");
            Assert.AreEqual(CrewInviteStatus.Available, _crewInviteStatusService.EvaluateForRemotePlayer(), "A player with Steam cannot invite from the lobby.");

            yield return AssertInactiveWithoutSteamCoroutine(terminals[0]);
            yield return FillCrewAndLetOneLeaveCoroutine();
        }

        public IEnumerator CheckClosedInFlightCoroutine() {
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => _crewRosterModel.IsJoinWindowOpen == false, "the crew roster closed in flight");
            Assert.AreEqual(CrewInviteStatus.NotAtStation, _crewInviteStatusService.EvaluateForRemotePlayer(), "A player can invite in flight.");
            AssertOneTerminalPerPad();
        }

        public IEnumerator CheckAtStationCoroutine(ShipLandingPad pad) {
            AssertOneTerminalPerPad();
            CrewInviteInteractable terminal = pad.GetComponentInChildren<CrewInviteInteractable>();
            Assert.IsNotNull(terminal, $"The pad {pad.name} has no crew invite terminal.");
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => _crewRosterModel.IsJoinWindowOpen, "the crew roster open at a station");
            Assert.AreEqual(CrewInviteStatus.Available, _crewInviteStatusService.EvaluateForRemotePlayer(), "A player with Steam cannot invite at a station.");
            yield return AssertInactiveWithoutSteamCoroutine(terminal);
        }

        public static void AssertOneTerminalPerPad() =>
            Assert.AreEqual(
                Object.FindObjectsByType<ShipLandingPad>(FindObjectsSortMode.None).Length,
                Object.FindObjectsByType<CrewInviteInteractable>(FindObjectsSortMode.None).Length,
                "Crew invite terminals do not match the pads one to one.");

        private IEnumerator AssertInactiveWithoutSteamCoroutine(CrewInviteInteractable terminal) {
            Assert.IsTrue(_steamLobby.IsBound, "The Steam lobby spy did not replace the scene binding.");
            Assert.AreEqual(CrewInviteStatus.NoSteam, _crewInviteStatusService.EvaluateForLocalPlayer(), "The test session unexpectedly has a Steam lobby.");
            int overlayRequests = _steamLobby.InviteOverlayRequests;
            try {
                CameraLookDriver.DebugPitchOverride = true;
                Vector3 front = terminal.transform.position - terminal.transform.forward * APPROACH_DISTANCE;
                yield return StandAtCoroutine(front + Vector3.up * STAND_HEIGHT);
                yield return LoopSmokeHandCheck.AimAtCoroutine(ConsoleCenter(terminal));
                Assert.AreSame(terminal, Interaction.Target.LookedAt, "The crew invite terminal is not under the crosshair.");
                Assert.IsNull(Interaction.Target.Usable, "The crew invite terminal is usable without Steam.");
                yield return LoopSmokeShopCheck.WaitForCoroutine(IsNoSteamTooltipShown, $"the tooltip \"{NO_STEAM_TEXT}\" on the terminal");
                Interaction.Interact();
                for (int i = 0; i < FRAMES_FOR_INTERACT_ROUND_TRIP; i++)
                    yield return null;

                Assert.AreEqual(overlayRequests, _steamLobby.InviteOverlayRequests, "Interact on the inactive terminal asked for the invite overlay.");
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }
        }

        // Fills the free slots with authenticated players and lets one go: the terminal must close at N/N and open
        // again for everyone without a rejoin.
        private IEnumerator FillCrewAndLetOneLeaveCoroutine() {
            List<LoopSmokeBot> fillers = new List<LoopSmokeBot>();
            try {
                for (int i = _crewRosterModel.PlayerCount; i < _playerSlots; i++)
                    fillers.Add(AuthenticateFiller(fillers.Count));

                yield return LoopSmokeShopCheck.WaitForCoroutine(() => IsRosterAt(_playerSlots), $"the crew roster at {_playerSlots}/{_playerSlots}");
                Assert.AreEqual(CrewInviteStatus.CrewFull, _crewInviteStatusService.EvaluateForRemotePlayer(), "The terminal stays available with a full crew.");

                fillers[0].Leave();
                fillers.RemoveAt(0);
                yield return LoopSmokeShopCheck.WaitForCoroutine(() => IsRosterAt(_playerSlots - 1), "the crew roster after one player left");
                Assert.AreEqual(CrewInviteStatus.Available, _crewInviteStatusService.EvaluateForRemotePlayer(), "The terminal does not open again after a player left.");
            } finally {
                foreach (LoopSmokeBot filler in fillers)
                    filler.Leave();
            }

            yield return LoopSmokeShopCheck.WaitForCoroutine(() => IsRosterAt(1), "the crew roster back to the host alone");
        }

        private static LoopSmokeBot AuthenticateFiller(int index) {
            LoopSmokeBot filler = LoopSmokeBot.Connect(FIRST_FILLER_CONNECTION_ID + index, FILLER_CLIENT_ID + index, FILLER_NAME + index);
            filler.Authenticate();
            Assert.IsTrue(filler.IsAuthenticated, $"The server turned away {FILLER_NAME}{index} in the lobby.");
            return filler;
        }

        private bool IsRosterAt(int playerCount) =>
            _crewRosterModel.IsAvailable && _crewRosterModel.PlayerCount == playerCount;

        private static bool IsNoSteamTooltipShown() {
            InteractionTooltipView view = Object.FindAnyObjectByType<InteractionTooltipView>();
            if (view == null || view.IsShown == false)
                return false;

            foreach (Text text in view.GetComponentsInChildren<Text>()) {
                if (text.text == NO_STEAM_TEXT)
                    return true;
            }

            return false;
        }

        private static IEnumerator StandAtCoroutine(Vector3 position) {
            Rigidbody body = Player.GetComponent<Features.CharacterMovableModule.Scripts.CharacterMovableBase>().Body;
            body.position = position;
            body.transform.position = position;
            body.linearVelocity = Vector3.zero;
            yield return new WaitForSeconds(STAND_SETTLE_SECONDS);
        }

        private static Vector3 ConsoleCenter(CrewInviteInteractable terminal) {
            foreach (Collider collider in terminal.GetComponentsInChildren<Collider>()) {
                if (collider.isTrigger == false)
                    return collider.bounds.center;
            }

            throw new InvalidOperationException("The crew invite terminal has no solid console.");
        }
    }
}
#endif
