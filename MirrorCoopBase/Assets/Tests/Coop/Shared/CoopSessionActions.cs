#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.LobbyModule.Scripts;
using Features.MenuModule.Scripts;
using Game.Connection;
using Mirror;
using Tests.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tests.Coop.Shared {
    // The menu and HUD steps a player takes to join and leave a direct-IP session, with the port and name the
    // coop test gives each process.
    public static class CoopSessionActions {
        public static void PrepareDirect(ushort port, string playerName) {
            ConnectionNetworkManager manager = SessionQueries.NetworkManager;
            manager.GetComponent<TelepathyTransport>().port = port;
            ((ConnectionAuthenticator)manager.authenticator).PlayerName = playerName;
        }

        public static void JoinThroughMenu(string address) {
            MenuSessionViewBase menu = Object.FindAnyObjectByType<MenuSessionViewBase>();
            menu.GetComponentInChildren<InputField>(true).text = address;
            menu.OnJoinClicked.Invoke();
        }

        public static void LeaveThroughHud() =>
            Object.FindAnyObjectByType<LeaveSessionViewBase>().OnLeaveClicked.Invoke();

        public static bool IsClientInLobby() {
            if (NetworkClient.isConnected == false || NetworkClient.ready == false || NetworkClient.connection.isAuthenticated == false)
                return false;

            ConnectionNetworkManager manager = SessionQueries.NetworkManager;
            bool isLobbyActive = SceneManager.GetActiveScene().name == manager.LobbySceneName;
            return isLobbyActive && manager.IsMapLoaded && SessionQueries.LocalPlayer != null;
        }

        public static bool IsRejectedInMenu() =>
            SessionQueries.Resolve<ConnectionRejectModel>().HasRejection && SessionQueries.IsBackInMenu();

        public static bool IsRejectTextShown(ConnectionRejectReason reason) {
            string expected = SessionQueries.Resolve<IConnectionRejectMessages>().Describe(reason);
            foreach (Text text in Object.FindAnyObjectByType<MenuSessionViewBase>().GetComponentsInChildren<Text>(true)) {
                if (text.text == expected)
                    return true;
            }

            return false;
        }
    }
}
#endif
