using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldStructurePool {
        public int ActiveCount { get; }
        public GameObject Take(GameObject prefab, Transform parent);
        public void ReturnAll();
    }
}
