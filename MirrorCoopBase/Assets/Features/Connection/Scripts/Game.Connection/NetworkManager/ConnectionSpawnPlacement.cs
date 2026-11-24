using System;
using Features.GameCoreModule.Contracts;
using Mirror;
using UnityEngine;

namespace Game.Connection
{
    // Where players appear: a running map's join place (the ship's deck), else the map's spawn point, else a Mirror
    // start position, else the origin.
    internal sealed class ConnectionSpawnPlacement
    {
        private readonly ConnectionSpawnModel _spawn;
        private readonly ISessionJoinSpawn _joinSpawn;
        private readonly Func<Transform> _getStartPosition;

        public ConnectionSpawnPlacement(ConnectionSpawnModel spawn, ISessionJoinSpawn joinSpawn, Func<Transform> getStartPosition)
        {
            _spawn = spawn;
            _joinSpawn = joinSpawn;
            _getStartPosition = getStartPosition;
        }

        public Pose GetPlayerPose() =>
            _joinSpawn.TryTakeJoinPose(out Pose pose) ? pose : GetSpawnPose();

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

        // Every player goes to the map's spawn on every peer: each owner moves its own body, the copies follow it.
        public void TeleportEverybody()
        {
            Pose pose = GetSpawnPose();
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null && conn.identity.TryGetComponent(out IPlayerBodyPlacement placement))
                    placement.ServerPlace(pose);
            }
        }
    }
}
