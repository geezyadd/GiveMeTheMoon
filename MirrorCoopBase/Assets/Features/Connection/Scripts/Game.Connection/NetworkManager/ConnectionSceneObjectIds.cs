using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Connection
{
    // Mirror bakes scene ids at build time, which Addressable scenes miss. Every peer derives the same id from the scene
    // name and the object's hierarchy path, so scene objects still match across the network.
    internal static class ConnectionSceneObjectIds
    {
        public static void Assign(Scene scene)
        {
            if (scene.IsValid() == false || scene.isLoaded == false)
                return;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                NetworkIdentity[] identities = roots[i].GetComponentsInChildren<NetworkIdentity>(true);
                for (int j = 0; j < identities.Length; j++)
                {
                    NetworkIdentity identity = identities[j];
                    if (identity.sceneId != 0)
                        continue;

                    identity.sceneId = StableSceneId(identity);
                }
            }
        }

        static ulong StableSceneId(NetworkIdentity identity)
        {
            string key = identity.gameObject.scene.name + ":" + GetHierarchyPath(identity.transform);
            unchecked
            {
                ulong hash = 2166136261;
                for (int i = 0; i < key.Length; i++)
                {
                    hash ^= key[i];
                    hash *= 16777619;
                }

                return hash == 0 ? 1UL : hash;
            }
        }

        static string GetHierarchyPath(Transform transform)
        {
            if (transform.parent == null)
                return transform.name;

            return GetHierarchyPath(transform.parent) + "/" + transform.name;
        }
    }
}
