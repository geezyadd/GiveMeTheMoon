using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldStructurePool {
        public GameObject Take(GameObject prefab, Transform parent);
        public void ReturnAll();
    }
}
