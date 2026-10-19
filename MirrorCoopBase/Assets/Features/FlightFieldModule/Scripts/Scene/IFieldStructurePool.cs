using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldStructurePool {
        public GameObject Take(GameObject prefab, Transform parent);
        // The prefab's root component on the taken instance, looked up once per instance.
        public T Take<T>(T prefab, Transform parent) where T : Component;
        public void Return(GameObject instance);
        public void ReturnAll();
    }
}
