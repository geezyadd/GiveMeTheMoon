using System;
using Mirror;
using UnityEngine;

namespace Game.Connection
{
    // Where players appear: the map's spawn point, else a Mirror start position, else the origin.
    internal sealed class ConnectionSpawnPlacement
    {
        private readonly ConnectionSpawnModel _spawn;
        private readonly Func<Transform> _getStartPosition;

        public ConnectionSpawnPlacement(ConnectionSpawnModel spawn, Func<Transform> getStartPosition)
        {
            _spawn = spawn;
            _getStartPosition = getStartPosition;
        }

        public Pose GetSpawnPose()
        {
            if (_spawn.HasSpawn)
                return new Pose(_spawn.Position, _spawn.Rotation);

            Transform start = _getStartPosition();
            return start != null
                ? new Pose(start.position, start.rotation)
                : new Pose(Vector3.zero, Quaternion.identity);
        }

        public void ClearSpawn()
        {
            _spawn.Clear();
        }

        public void TeleportEverybody()
        {
            Pose pose = GetSpawnPose();
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null)
                    continue;

                NetworkServer.SendToReady(new TeleportMessage
                {
                    netId = conn.identity.netId,
                    position = pose.position,
                    rotation = pose.rotation
                });
            }
        }

        public static void OnTeleportMessage(TeleportMessage msg)
        {
            if (!NetworkClient.spawned.TryGetValue(msg.netId, out NetworkIdentity identity) || identity == null)
                return;

            ApplyTeleport(identity.transform, msg.position, msg.rotation);
        }

        static void ApplyTeleport(Transform target, Vector3 position, Quaternion rotation)
        {
            CharacterController controller = target.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;

            Rigidbody rigidbody = target.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.position = position;
                rigidbody.rotation = rotation;
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            target.SetPositionAndRotation(position, rotation);

            if (controller != null)
                controller.enabled = true;
        }
    }
}
