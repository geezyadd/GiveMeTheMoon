using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Local field objects (structure triggers and single visual rocks) kept between fields: a returned object is only
    // deactivated and stays where it is, so the next field takes it without Instantiate.
    public sealed class FieldStructurePool : IFieldStructurePool {
        private readonly Dictionary<GameObject, Stack<GameObject>> _freeByPrefab = new();
        private readonly Dictionary<GameObject, GameObject> _activePrefabByInstance = new();
        // Read once when a rock is made, so taking a pooled rock does not look its component up again.
        private readonly Dictionary<GameObject, FieldRockVisual> _rockVisualByInstance = new();

        // A free instance is moved under the given parent, so it ends up where a new one would be made.
        public GameObject Take(GameObject prefab, Transform parent) {
            GameObject instance = TakeFree(prefab);
            if (instance == null)
                instance = Object.Instantiate(prefab, parent, false);
            else
                instance.transform.SetParent(parent, false);

            _activePrefabByInstance.Add(instance, prefab);
            return instance;
        }

        public FieldRockVisual TakeRock(FieldRockVisual prefab, Transform parent) {
            GameObject instance = Take(prefab.gameObject, parent);
            if (!_rockVisualByInstance.TryGetValue(instance, out FieldRockVisual rockVisual)) {
                rockVisual = instance.GetComponent<FieldRockVisual>();
                _rockVisualByInstance.Add(instance, rockVisual);
            }

            return rockVisual;
        }

        public void Return(GameObject instance) {
            if (!_activePrefabByInstance.Remove(instance, out GameObject prefab))
                throw new KeyNotFoundException($"{instance} was not taken from the field pool or is already returned.");

            PushFree(instance, prefab);
        }

        public void ReturnAll() {
            foreach (KeyValuePair<GameObject, GameObject> active in _activePrefabByInstance)
                PushFree(active.Key, active.Value);

            _activePrefabByInstance.Clear();
        }

        // The root is kept across scenes, but an object destroyed from outside must not come back from the pool.
        private GameObject TakeFree(GameObject prefab) {
            if (!_freeByPrefab.TryGetValue(prefab, out Stack<GameObject> free))
                return null;

            while (free.Count > 0) {
                GameObject instance = free.Pop();
                if (instance != null)
                    return instance;
            }

            return null;
        }

        private void PushFree(GameObject instance, GameObject prefab) {
            if (instance == null)
                return;

            instance.SetActive(false);
            if (!_freeByPrefab.TryGetValue(prefab, out Stack<GameObject> free)) {
                free = new Stack<GameObject>();
                _freeByPrefab.Add(prefab, free);
            }

            free.Push(instance);
        }
    }
}
