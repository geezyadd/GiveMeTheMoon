#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.MenuModule.Scripts;
using Game.Connection;
using Mirror;
using Tests.Shared;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // A player turned away by the host reads why in the menu. From the menu the test joins a "host" on this machine:
    // a loopback socket that takes the connection and never answers, so the client stays connected and waits, with
    // no dependence on routing. The test then hands the client the host's "in flight" answer through the transport's
    // receive callback: the client's real authenticator, the return to the menu and the menu window do the rest.
    public sealed class LoopSmokeRejectCheck {
        private const string LOOPBACK_ADDRESS = "127.0.0.1";
        private const string SCREENSHOT_FOLDER = "Temp/LoopSmoke";
        private const string REJECT_SCREENSHOT = "join-rejected-in-flight.png";

        public IEnumerator JoinRejectedInFlightCoroutine() {
            string expected = SessionQueries.Resolve<IConnectionRejectMessages>().Describe(ConnectionRejectReason.InFlight);
            TelepathyTransport telepathy = Object.FindAnyObjectByType<TelepathyTransport>(FindObjectsInactive.Include);
            ushort shippedPort = telepathy.port;
            TcpListener silentHost = new TcpListener(IPAddress.Loopback, 0);
            silentHost.Start();
            try {
                telepathy.port = (ushort)((IPEndPoint)silentHost.LocalEndpoint).Port;
                Object.FindAnyObjectByType<InputField>().text = LOOPBACK_ADDRESS;
                Object.FindAnyObjectByType<MenuSessionViewBase>().OnJoinClicked.Invoke();
                yield return LoopSmokeShopCheck.WaitForCoroutine(IsWaitingForHost, "the client connected and waiting for the host's answer");

                LoopSmokeTransportInput.Deliver(
                    new ConnectionAuthenticator.AuthResponseMessage { rejection = (byte)ConnectionRejectReason.InFlight },
                    Transport.active.OnClientDataReceived);
                yield return LoopSmokeShopCheck.WaitForCoroutine(IsBackInMenu, "the menu after the host turned the player away");
                yield return LoopSmokeShopCheck.WaitForCoroutine(() => IsShown(expected), $"the menu text \"{expected}\"");
                yield return CaptureScreenshotCoroutine();
            } finally {
                telepathy.port = shippedPort;
                silentHost.Stop();
            }
        }

        // The host's answer takes a network round trip: the client is in the session state by then.
        private static bool IsWaitingForHost() =>
            NetworkClient.isConnected && NetworkServer.active == false && IsFlowSettledIn(typeof(SessionGameFlowState));

        private static bool IsBackInMenu() =>
            NetworkClient.active == false && IsFlowSettledIn(typeof(MenuGameFlowState));

        private static bool IsFlowSettledIn(Type stateType) {
            IGameFlowStateMachineService flow = SessionQueries.Resolve<IGameFlowStateMachineService>();
            return flow.IsTransitioning == false && flow.CurrentStateType == stateType;
        }

        private static bool IsShown(string message) {
            foreach (Text text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)) {
                if (text.isActiveAndEnabled && text.text == message)
                    return true;
            }

            return false;
        }

        // Kept in the project's Temp folder for a human to look at; the test does not compare images.
        private static IEnumerator CaptureScreenshotCoroutine() {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SCREENSHOT_FOLDER));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, REJECT_SCREENSHOT));
            yield return null;
            yield return null;
        }
    }
}
#endif
