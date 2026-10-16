using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    public interface IFieldStructurePool {
        public GameObject Take(GameObject prefab, Transform parent);
        public FieldRockVisual TakeRock(FieldRockVisual prefab, Transform parent);
        public void Return(GameObject instance);
        public void ReturnAll();
    }
}
