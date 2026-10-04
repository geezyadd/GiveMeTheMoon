using UnityEngine;
using Zenject;

namespace Game.Connection
{
    public class ConnectionSpawnPoint : MonoBehaviour
    {
        ConnectionSpawnModel spawn;

        [Inject]
        void Construct(ConnectionSpawnModel spawnModel)
        {
            spawn = spawnModel;
            Apply();
        }

        public void Apply()
        {
            spawn.Set(transform.position, transform.rotation);
        }
    }
}
