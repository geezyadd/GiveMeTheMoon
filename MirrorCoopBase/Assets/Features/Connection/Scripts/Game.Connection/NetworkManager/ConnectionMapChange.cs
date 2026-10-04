using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Connection
{
    // Moves every peer to a new map together: each peer loads it additively and reports back; once all have, the server
    // teleports the players to the spawn and everybody unloads the old map.
    internal sealed class ConnectionMapChange
    {
        private readonly MonoBehaviour _coroutineHost;
        private readonly ConnectionSceneNames _sceneNames;
        private readonly ConnectionSceneLoading _sceneLoading;
        private readonly ConnectionSpawnPlacement _spawnPlacement;
        private readonly ConnectionPersistentScene _persistentScene;
        private readonly ConnectionNetworkEvents _events;
        private readonly float _everyoneReadyDelay;
        private readonly Action _clientSceneChanged;
        private readonly Action _loadFailed;

        private Dictionary<NetworkConnectionToClient, bool> _mapLoadedByConnection;
        private string _sceneToUnload;
        private bool _lookForReadyPlayers;

        public ConnectionMapChange(
            MonoBehaviour coroutineHost,
            ConnectionSceneNames sceneNames,
            ConnectionSceneLoading sceneLoading,
            ConnectionSpawnPlacement spawnPlacement,
            ConnectionPersistentScene persistentScene,
            ConnectionNetworkEvents events,
            float everyoneReadyDelay,
            Action clientSceneChanged,
            Action loadFailed)
        {
            _coroutineHost = coroutineHost;
            _sceneNames = sceneNames;
            _sceneLoading = sceneLoading;
            _spawnPlacement = spawnPlacement;
            _persistentScene = persistentScene;
            _events = events;
            _everyoneReadyDelay = everyoneReadyDelay;
            _clientSceneChanged = clientSceneChanged;
            _loadFailed = loadFailed;
        }

        public bool IsMapLoaded { get; private set; }
        public bool IsChangingMap { get; private set; }

        public void StartServer()
        {
            _sceneToUnload = SceneManager.GetActiveScene().name;
            _mapLoadedByConnection = new Dictionary<NetworkConnectionToClient, bool>();
            NetworkServer.RegisterHandler<MapLoadedMessage>(OnClientMapLoaded);
            if (SceneManager.GetActiveScene().name == _sceneNames.Lobby)
                IsMapLoaded = true;
        }

        public void StopServer()
        {
            NetworkServer.UnregisterHandler<MapLoadedMessage>();
            IsMapLoaded = false;
            IsChangingMap = false;
            _lookForReadyPlayers = false;
            _mapLoadedByConnection = null;
        }

        public void StartClient()
        {
            NetworkClient.ReplaceHandler<SceneMessage>(OnMirrorSceneMessage);
            NetworkClient.RegisterHandler<EveryoneIsReadyMessage>(OnEveryoneIsReady);
            NetworkClient.RegisterHandler<SceneChangeMessage>(OnSceneChangeMessage);
            if (SceneManager.GetActiveScene().name == _sceneNames.Lobby)
                IsMapLoaded = true;
        }

        public void StopClient()
        {
            NetworkClient.UnregisterHandler<SceneMessage>();
            NetworkClient.UnregisterHandler<EveryoneIsReadyMessage>();
            NetworkClient.UnregisterHandler<SceneChangeMessage>();
            _lookForReadyPlayers = false;
            _sceneToUnload = string.Empty;
            IsMapLoaded = false;
            IsChangingMap = false;
        }

        public void MarkMapUnloaded()
        {
            IsMapLoaded = false;
        }

        public void AddPlayer(NetworkConnectionToClient conn)
        {
            if (_mapLoadedByConnection != null)
                _mapLoadedByConnection[conn] = false;
        }

        public void ForgetConnection(NetworkConnectionToClient conn)
        {
            _mapLoadedByConnection?.Remove(conn);
        }

        // A player who left no longer holds up the others.
        public void RecheckReadyPlayers()
        {
            if (_lookForReadyPlayers)
                CheckIfEverybodyIsReady();
        }

        public void UnloadAfterChange(string sceneName)
        {
            _sceneToUnload = sceneName;
        }

        // Server only; the caller has already set NetworkManager.networkSceneName.
        public void Begin(string newSceneName)
        {
            _lookForReadyPlayers = true;
            IsChangingMap = true;
            _events.RaiseMapLoadStarted();
            NetworkServer.SendToAll(new SceneChangeMessage
            {
                sceneName = newSceneName,
                operation = SceneOperation.LoadAdditive
            });
        }

        void OnMirrorSceneMessage(SceneMessage msg)
        {
            _coroutineHost.StartCoroutine(HandleMirrorSceneMessage(msg));
        }

        // Mirror's own scene message reaches a client that joins after the map change; load the scene through Addressables.
        IEnumerator HandleMirrorSceneMessage(SceneMessage msg)
        {
            if (NetworkServer.active)
                yield break;

            if (msg.sceneOperation == SceneOperation.UnloadAdditive)
            {
                yield return _sceneLoading.UnloadIfLoaded(msg.sceneName);
                NetworkClient.isLoadingScene = false;
                yield break;
            }

            Scene existing = SceneManager.GetSceneByName(msg.sceneName);
            if (existing.IsValid() && existing.isLoaded)
            {
                SceneManager.SetActiveScene(existing);
                NetworkClient.isLoadingScene = false;
                _clientSceneChanged();
                yield break;
            }

            NetworkClient.isLoadingScene = true;
            yield return _sceneLoading.Load(msg.sceneName);

            Scene loaded = SceneManager.GetSceneByName(msg.sceneName);
            if (loaded.IsValid())
                SceneManager.SetActiveScene(loaded);

            NetworkClient.isLoadingScene = false;
            _clientSceneChanged();
        }

        void OnSceneChangeMessage(SceneChangeMessage msg)
        {
            _coroutineHost.StartCoroutine(HandleSceneChange(msg));
        }

        IEnumerator HandleSceneChange(SceneChangeMessage msg)
        {
            if (msg.operation == SceneOperation.LoadAdditive)
            {
                IsMapLoaded = false;
                _spawnPlacement.ClearSpawn();
                // Spawn messages that arrive before this scene's NetworkIdentities are
                // registered are dropped. Pause the inbox for the whole load, including
                // the wait, and resume only after they are registered.
                BeginSceneLoadPause();
                yield return new WaitForSeconds(0.1f);
                _events.RaiseMapUnloading(SceneManager.GetActiveScene().name);
            }

            // Kept as one coroutine: a nested coroutine per branch could shift a step by a frame.
            switch (msg.operation)
            {
                case SceneOperation.LoadAdditive:
                    Scene loaded = SceneManager.GetSceneByName(msg.sceneName);
                    if (loaded.IsValid() == false)
                    {
                        yield return _sceneLoading.Load(msg.sceneName);

                        loaded = SceneManager.GetSceneByName(msg.sceneName);
                        if (loaded.IsValid() == false)
                        {
                            EndSceneLoadPause();
                            _loadFailed();
                            yield break;
                        }

                        SceneManager.SetActiveScene(loaded);
                    }

                    ConnectionSceneObjectIds.Assign(loaded);
                    if (NetworkClient.active)
                        NetworkClient.PrepareToSpawnSceneObjects();
                    if (NetworkServer.active)
                        NetworkServer.SpawnObjects();

                    EndSceneLoadPause();
                    NetworkClient.Send(new MapLoadedMessage());
                    break;

                case SceneOperation.UnloadAdditive:
                    if (NetworkServer.active)
                        ConnectionPersistentScene.DestroyMapObjects(msg.sceneName);
                    _persistentScene.MovePlayersToPersistentScene();
                    yield return _sceneLoading.UnloadIfLoaded(msg.sceneName);
                    Scene leftover = SceneManager.GetSceneByName(msg.sceneName);
                    if (leftover.IsValid() && leftover.isLoaded)
                        yield return SceneManager.UnloadSceneAsync(leftover);
                    break;
            }
        }

        static void BeginSceneLoadPause()
        {
            if (NetworkClient.active)
                NetworkClient.isLoadingScene = true;
            if (NetworkServer.active)
                NetworkServer.isLoadingScene = true;
        }

        static void EndSceneLoadPause()
        {
            if (NetworkClient.active)
                NetworkClient.isLoadingScene = false;
            if (NetworkServer.active)
                NetworkServer.isLoadingScene = false;
        }

        void OnClientMapLoaded(NetworkConnectionToClient conn, MapLoadedMessage _)
        {
            if (_mapLoadedByConnection == null)
                return;

            _mapLoadedByConnection[conn] = true;
            if (_lookForReadyPlayers)
                CheckIfEverybodyIsReady();
        }

        void CheckIfEverybodyIsReady()
        {
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (!_mapLoadedByConnection.TryGetValue(conn, out bool loaded) || !loaded)
                    return;
            }

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values.ToArray())
                _mapLoadedByConnection[conn] = false;

            _spawnPlacement.TeleportEverybody();
            _coroutineHost.StartCoroutine(SendEveryoneReady());
        }

        IEnumerator SendEveryoneReady()
        {
            yield return new WaitForSeconds(_everyoneReadyDelay);
            NetworkServer.SendToAll(new EveryoneIsReadyMessage { sceneToUnload = _sceneToUnload });
            _sceneToUnload = NetworkManager.networkSceneName;
        }

        void OnEveryoneIsReady(EveryoneIsReadyMessage ready)
        {
            if (NetworkServer.active && !string.IsNullOrEmpty(ready.sceneToUnload))
            {
                NetworkServer.SendToAll(new SceneChangeMessage
                {
                    sceneName = ready.sceneToUnload,
                    operation = SceneOperation.UnloadAdditive
                });
            }

            _coroutineHost.StartCoroutine(FinishMapReady());
        }

        IEnumerator FinishMapReady()
        {
            yield return new WaitForSeconds(_everyoneReadyDelay);
            IsMapLoaded = true;
            IsChangingMap = false;
            _lookForReadyPlayers = false;
            if (NetworkServer.active)
                NetworkServer.isLoadingScene = false;
            _events.RaiseMapReady();
        }
    }
}
