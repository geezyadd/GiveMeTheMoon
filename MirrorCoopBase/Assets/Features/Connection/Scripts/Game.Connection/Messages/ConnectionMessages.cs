using Mirror;
using UnityEngine;

namespace Game.Connection
{
    public struct SceneChangeMessage : NetworkMessage
    {
        public string sceneName;
        public SceneOperation operation;
    }

    public struct MapLoadedMessage : NetworkMessage { }

    public struct EveryoneIsReadyMessage : NetworkMessage
    {
        public string sceneToUnload;
    }

    public struct TeleportMessage : NetworkMessage
    {
        public uint netId;
        public Vector3 position;
        public Quaternion rotation;
    }
}
