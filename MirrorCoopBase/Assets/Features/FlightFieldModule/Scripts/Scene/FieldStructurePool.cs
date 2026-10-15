using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Local structure objects kept between fields: a returned object is only deactivated and stays under the field
    // root, so the next field takes it without Instantiate.
    public sealed class FieldStructurePool : IFieldStructurePool {
        private readonly Dictionary<GameObject, Stack<GameObject>> _freeByPrefab = new();
        private readonly List<GameObject> _activeInstances = new();
        private readonly List<GameObject> _activePrefabs = new();

        public GameObject Take(GameObject prefab, Transform parent) {
            GameObject instance = TakeFree(prefab);
            if (instance == null)
                instance = Object.Instantiate(prefab, parent, false);

            _activeInstances.Add(instance);
            _activePrefabs.Add(prefab);
            return instance;
        }

        public void ReturnAll() {
            for (int i = 0; i < _activeInstances.Count; i++)
                Return(_activeInstances[i], _activePrefabs[i]);

            _activeInstances.Clear();
            _activePrefabs.Clear();
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

        private void Return(GameObject instance, GameObject prefab) {
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
