using System;
using System.Threading.Tasks;
using Features.SceneLoaderModule.Scripts;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game.Connection
{
    // Mirror's entry point for the session: receives the network callbacks and hands the work to the map change,
    // spawn placement, persistent scene, transport switch and menu return.
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/Connection Network Manager")]
    public class ConnectionNetworkManager : NetworkManager
    {
        [Header("Connection Scenes")]
        [SerializeField] string lobbySceneName = "LobbyScene";
        [SerializeField] string menuSceneName = "MenuScene";
        [SerializeField] string gameSceneName = "GameScene";
        [SerializeField] string persistentSceneName = "GlobalScene";

        [Header("Map Change")]
        [SerializeField] float everyoneReadyDelay = 0.5f;

        [Header("Transports")]
        [SerializeField] Transport telepathyTransport;
        [SerializeField] Transport fizzyTransport;

        ConnectionSessionModel sessionModel;
        ConnectionNetworkEvents events;
        ConnectionTransportSwitch transportSwitch;
        ConnectionSceneLoading sceneLoading;
        ConnectionSpawnPlacement spawnPlacement;
        ConnectionPersistentScene persistentScene;
        ConnectionMapChange mapChange;
        ConnectionMenuReturn menuReturn;
        bool returningToMenu;

        public string LobbySceneName => lobbySceneName;
        public string MenuSceneName => menuSceneName;
        public string GameSceneName => gameSceneName;
        public bool IsMapLoaded => mapChange.IsMapLoaded;
        public bool IsChangingMap => mapChange.IsChangingMap;
        public bool UsesSteamTransport => transportSwitch.UsesSteam;

        public bool IsJoinable =>
            string.IsNullOrEmpty(networkSceneName) || networkSceneName == lobbySceneName;

        [Inject]
        void Construct(
            ISceneLoaderService sceneLoaderService,
            ConnectionConfig connectionConfig,
            ConnectionSessionModel connectionSessionModel,
            ConnectionSpawnModel spawnModel,
            ConnectionNetworkEvents networkEvents)
        {
            sessionModel = connectionSessionModel;
            events = networkEvents;

            if (connectionConfig != null)
            {
                maxConnections = connectionConfig.MaxConnections;
                everyoneReadyDelay = connectionConfig.EveryoneReadyDelay;
            }

            var sceneNames = new ConnectionSceneNames(lobbySceneName, menuSceneName, gameSceneName, persistentSceneName);
            sceneLoading = new ConnectionSceneLoading(sceneLoaderService);
            spawnPlacement = new ConnectionSpawnPlacement(spawnModel, GetStartPosition);
            persistentScene = new ConnectionPersistentScene(sceneNames, playerPrefab);
            menuReturn = new ConnectionMenuReturn(sceneLoading, sceneNames);
            mapChange = new ConnectionMapChange(
                this,
                sceneNames,
                sceneLoading,
                spawnPlacement,
                persistentScene,
                networkEvents,
                everyoneReadyDelay,
                OnClientSceneChanged,
                StopSessionAndReturnToMenu);

            RegisterInModel();
        }

        public override void Awake()
        {
            transportSwitch = new ConnectionTransportSwitch(gameObject, telepathyTransport, fizzyTransport);
            if (transport == null)
                transport = transportSwitch.Telepathy;

            offlineScene = string.Empty;
            onlineScene = string.Empty;
            base.Awake();
        }

        void OnEnable()
        {
            RegisterInModel();
        }

        void OnDisable()
        {
            if (sessionModel == null || sessionModel.NetworkManager != this)
                return;

            sessionModel.NetworkManager = null;
        }

        void Reset()
        {
            maxConnections = 4;
            dontDestroyOnLoad = true;
            autoCreatePlayer = true;
            offlineScene = string.Empty;
            onlineScene = string.Empty;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            mapChange.StartServer();
            events.RaiseServerStarted();
        }

        public override void OnStopServer()
        {
            mapChange.StopServer();
            spawnPlacement.ClearSpawn();
            events.RaiseServerStopped();
            base.OnStopServer();
            ResetTransportIfIdle();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            mapChange.StartClient();
            NetworkClient.RegisterHandler<TeleportMessage>(ConnectionSpawnPlacement.OnTeleportMessage);
            events.RaiseClientStarted();
        }

        public override void OnStopClient()
        {
            mapChange.StopClient();
            NetworkClient.UnregisterHandler<TeleportMessage>();
            loadingSceneAsync = null;
            networkSceneName = string.Empty;
            spawnPlacement.ClearSpawn();
            events.RaiseClientStopped();
            base.OnStopClient();
            ResetTransportIfIdle();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            mapChange.ForgetConnection(conn);
            base.OnServerDisconnect(conn);
            mapChange.RecheckReadyPlayers();
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Pose spawn = spawnPlacement.GetSpawnPose();
            GameObject player = Instantiate(playerPrefab, spawn.position, spawn.rotation);
            player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
            NetworkServer.AddPlayerForConnection(conn, player);
            persistentScene.MoveToPersistentScene(player);
            mapChange.AddPlayer(conn);
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            if (!returningToMenu)
                StopSessionAndReturnToMenu();
        }

        public async Task PrepareLobbySceneAsync()
        {
            if (string.IsNullOrEmpty(lobbySceneName))
                return;

            await sceneLoading.LoadAndActivateAsync(lobbySceneName);
        }

        public Task UnloadSceneIfLoadedAsync(string sceneName) =>
            sceneLoading.UnloadIfLoadedAsync(sceneName);

        // The scene change also goes to the host's own client; before it is authenticated it drops the message and disconnects.
        public bool CanStartGame()
        {
            return NetworkServer.active && !IsChangingMap && NetworkClient.ready && NetworkClient.connection.isAuthenticated;
        }

        public void StartGame()
        {
            if (!NetworkServer.active)
                return;

            if (!CanStartGame())
                return;

            if (string.IsNullOrWhiteSpace(gameSceneName))
                return;

            Scene gameScene = SceneManager.GetSceneByName(gameSceneName);
            if (gameScene.IsValid() && gameScene.isLoaded)
                return;

            mapChange.UnloadAfterChange(lobbySceneName);
            ChangeMap(gameSceneName);
        }

        public void ChangeMap(string newSceneName)
        {
            if (!NetworkServer.active)
            {
                Debug.LogError("ChangeMap can only be called on the server.");
                return;
            }

            if (string.IsNullOrWhiteSpace(newSceneName))
            {
                Debug.LogError("ChangeMap empty scene name.");
                return;
            }

            if (NetworkServer.isLoadingScene && newSceneName == networkSceneName)
            {
                Debug.LogError($"Scene change is already in progress for {newSceneName}");
                return;
            }

            // Only NetworkManager can set networkSceneName, so it stays here.
            networkSceneName = newSceneName;
            mapChange.Begin(newSceneName);
        }

        public bool ReturnToLobby()
        {
            if (!NetworkServer.active)
                return false;

            if (SceneManager.GetActiveScene().name == lobbySceneName)
                return false;

            ChangeMap(lobbySceneName);
            return true;
        }

        public void SetUseSteamTransport(bool useSteam)
        {
            transport = transportSwitch.Select(useSteam);
            Transport.active = transport;
        }

        // Fire-and-forget entry for network callbacks; a failed scene unload would otherwise be lost silently.
        public async void StopSessionAndReturnToMenu()
        {
            try
            {
                await StopSessionToMenuAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public async Task StopSessionToMenuAsync()
        {
            if (returningToMenu)
                return;

            returningToMenu = true;
            try
            {
                StopHost();
                mapChange.MarkMapUnloaded();
                await menuReturn.ReturnAsync();
            }
            finally
            {
                returningToMenu = false;
            }
        }

        void ResetTransportIfIdle()
        {
            if (UsesSteamTransport == false)
                return;
            if (NetworkServer.active || NetworkClient.active)
                return;

            try
            {
                SetUseSteamTransport(false);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        void RegisterInModel()
        {
            if (sessionModel == null)
                return;

            sessionModel.NetworkManager = this;
        }
    }
}
