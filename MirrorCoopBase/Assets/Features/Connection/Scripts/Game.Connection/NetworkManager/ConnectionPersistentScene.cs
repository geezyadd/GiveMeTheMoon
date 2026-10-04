using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Connection
{
    // Players live in the persistent scene so a map unload does not take them along; map leftovers are destroyed instead.
    internal sealed class ConnectionPersistentScene
    {
        private readonly ConnectionSceneNames _sceneNames;
        private readonly GameObject _playerPrefab;

        public ConnectionPersistentScene(ConnectionSceneNames sceneNames, GameObject playerPrefab)
        {
            _sceneNames = sceneNames;
            _playerPrefab = playerPrefab;
        }

        public void MoveToPersistentScene(GameObject target)
        {
            if (target == null)
                return;

            Scene persistent = SceneManager.GetSceneByName(_sceneNames.Persistent);
            if (persistent.IsValid() && persistent.isLoaded)
            {
                if (target.scene != persistent)
                    SceneManager.MoveGameObjectToScene(target, persistent);
                return;
            }

            Object.DontDestroyOnLoad(target);
        }

        public void MovePlayersToPersistentScene()
        {
            if (NetworkServer.active)
            {
                foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
                {
                    if (conn.identity != null)
                        MoveToPersistentScene(conn.identity.gameObject);
                }
            }

            if (NetworkClient.active == false)
                return;

            // connectionToClient exists only on the server: a client recognises remote players by the player prefab.
            uint playerAssetId = _playerPrefab.GetComponent<NetworkIdentity>().assetId;
            foreach (NetworkIdentity identity in NetworkClient.spawned.Values)
            {
                if (identity == null || identity.assetId != playerAssetId)
                    continue;

                MoveToPersistentScene(identity.gameObject);
            }
        }

        // Pads, drops, items and wrecks are spawned into the map scene; destroy them through Mirror so clients drop them too
        // and nothing from this map survives into the next one. Scene objects still go with the scene unload.
        public static void DestroyMapObjects(string sceneName)
        {
            foreach (NetworkIdentity identity in NetworkServer.spawned.Values.ToArray())
            {
                if (IsMapRuntimeObject(identity, sceneName) == false)
                    continue;

                NetworkServer.Destroy(identity.gameObject);
            }
        }

        static bool IsMapRuntimeObject(NetworkIdentity identity, string sceneName) =>
            identity != null
            && identity.sceneId == 0
            && IsPlayerObject(identity) == false
            && identity.gameObject.scene.name == sceneName;

        static bool IsPlayerObject(NetworkIdentity identity) =>
            identity.connectionToClient != null && identity.connectionToClient.identity == identity;
    }
}
