#if UNITY_INCLUDE_TESTS
using Features.GrabModule.Scripts;
using Features.LobbyModule.Scripts;
using Features.ShipModule.Scripts;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // What a host test does on the server side of the loop: shorter flight timings, the engines, the launch lever.
    // Shared by LoopSmokeTest and the two-process coop test.
    public static class LoopSmokeHostSteps {
        public const int REQUIRED_ENGINES = 2;
        public const string ROCKET_ENGINE_MODULE_ID = "rocket-engine";
        // Only timings are shortened; distances, heights and pads stay as shipped.
        private const string FAST_TIMINGS_JSON =
            "{\"_takeoffSeconds\":1.5,\"_cruiseSeconds\":2,\"_landingSeconds\":1.5,\"_perLoopCruiseSeconds\":0}";

        // Binds a copy of ShipRunConfig with the fast timings in place of the asset; the caller destroys the copy.
        public static ShipRunConfig BindFastTimings(DiContainer container) {
            ShipRunConfig fastConfig = Object.Instantiate(container.Resolve<ShipRunConfig>());
            JsonUtility.FromJsonOverwrite(FAST_TIMINGS_JSON, fastConfig);
            container.Unbind<ShipRunConfig>();
            container.Bind<ShipRunConfig>().FromInstance(fastConfig);
            return fastConfig;
        }

        // Start sends a scene change to every client; the host's own client drops it until it is authenticated.
        public static bool IsLobbyReady() {
            StartGameViewBase start = Object.FindAnyObjectByType<StartGameViewBase>();
            bool isHostClientReady = NetworkServer.active && NetworkClient.ready && NetworkClient.connection.isAuthenticated;
            return isHostClientReady && start != null && start.OnStartClicked != null;
        }

        public static void ClickStart() =>
            Object.FindAnyObjectByType<StartGameViewBase>().OnStartClicked.Invoke();

        public static void InstallRequiredEngines(ShipBase ship) {
            int installed = 0;
            foreach (ShipSocket socket in ship.Sockets) {
                if (socket.RequiredForLaunch == false)
                    continue;

                Assert.AreEqual(ShipModuleType.Engine, socket.AcceptedType, $"Required socket {socket.name} does not take an engine.");
                Assert.IsTrue(socket.ServerTryInstall(ROCKET_ENGINE_MODULE_ID), $"Engine install failed on {socket.name}.");
                installed++;
            }

            Assert.AreEqual(REQUIRED_ENGINES, installed, "Unexpected number of required engine sockets.");
            Assert.IsTrue(ship.CanLaunch, "The ship cannot launch with both engines installed.");
        }

        public static int CountInstalledEngines(ShipBase ship) {
            int count = 0;
            foreach (ShipSocket socket in ship.Sockets) {
                if (socket.RequiredForLaunch && socket.InstalledModuleId == ROCKET_ENGINE_MODULE_ID)
                    count++;
            }

            return count;
        }

        public static void PullLaunchLever() {
            ShipLaunchInteractable lever = Object.FindAnyObjectByType<ShipLaunchInteractable>();
            NetworkIdentity user = SessionQueries.LocalPlayer;
            GrabController grab = user.GetComponentInChildren<GrabController>();
            Assert.IsTrue(lever.CanUse(user, grab), "The launch lever cannot be used.");
            lever.ServerUse(user, grab);
        }
    }
}
#endif
