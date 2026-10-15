#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.MenuModule.Scripts;
using Game.Connection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // A player turned away by the host reads why in the menu. From the menu the test joins an address that never
    // answers, then hands the client the host's "in flight" answer through the transport's receive callback: the
    // client's real authenticator, the return to the menu and the menu window do the rest.
    public sealed class LoopSmokeRejectCheck {
        // Not routable: the connect attempt hangs instead of failing at once, so only the injected answer ends it.
        private const string SILENT_ADDRESS = "10.255.255.1";
        private const int BATCH_THRESHOLD = 1200;
        private const string SCREENSHOT_FOLDER = "Temp/LoopSmoke";
        private const string REJECT_SCREENSHOT = "join-rejected-in-flight.png";

        public IEnumerator JoinRejectedInFlightCoroutine() {
            string expected = LoopSmokeShopCheck.Resolve<IConnectionRejectMessages>().Describe(ConnectionRejectReason.InFlight);
            MenuSessionViewBase menu = Object.FindAnyObjectByType<MenuSessionViewBase>();
            Object.FindAnyObjectByType<InputField>().text = SILENT_ADDRESS;
            menu.OnJoinClicked.Invoke();
            yield return LoopSmokeShopCheck.WaitForCoroutine(IsConnecting, "the client connecting to the host in the session state");

            SendHostAnswer(ConnectionRejectReason.InFlight);
            yield return LoopSmokeShopCheck.WaitForCoroutine(IsBackInMenu, "the menu after the host turned the player away");
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => IsShown(expected), $"the menu text \"{expected}\"");
            yield return CaptureScreenshotCoroutine();
        }

        // The host's answer takes a network round trip: the client is in the session state by then.
        private static bool IsConnecting() =>
            NetworkClient.active && NetworkClient.connection != null && NetworkServer.active == false
            && IsFlowSettledIn(typeof(SessionGameFlowState));

        private static bool IsBackInMenu() =>
            NetworkClient.active == false && IsFlowSettledIn(typeof(MenuGameFlowState));

        private static bool IsFlowSettledIn(Type stateType) {
            IGameFlowStateMachineService flow = LoopSmokeShopCheck.Resolve<IGameFlowStateMachineService>();
            return flow.IsTransitioning == false && flow.CurrentStateType == stateType;
        }

        private static bool IsShown(string message) {
            foreach (Text text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)) {
                if (text.isActiveAndEnabled && text.text == message)
                    return true;
            }

            return false;
        }

        private static void SendHostAnswer(ConnectionRejectReason reason) {
            using NetworkWriterPooled packed = NetworkWriterPool.Get();
            NetworkMessages.Pack(new ConnectionAuthenticator.AuthResponseMessage { rejection = (byte)reason }, packed);
            Batcher batcher = new Batcher(BATCH_THRESHOLD);
            batcher.AddMessage(packed.ToArraySegment(), NetworkTime.localTime);
            using NetworkWriterPooled batch = NetworkWriterPool.Get();
            Assert.IsTrue(batcher.GetBatch(batch), "The host's answer could not be batched.");
            Transport.active.OnClientDataReceived.Invoke(batch.ToArraySegment(), Channels.Reliable);
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
