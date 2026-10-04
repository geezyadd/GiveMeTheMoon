using UnityEngine;

namespace Game.Connection
{
    // Where the server places joining players and teleports everybody after a map change.
    public sealed class ConnectionSpawnModel
    {
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; } = Quaternion.identity;
        public bool HasSpawn { get; private set; }

        public void Set(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
            HasSpawn = true;
        }

        public void Clear()
        {
            Position = default;
            Rotation = Quaternion.identity;
            HasSpawn = false;
        }
    }
}
