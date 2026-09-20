using System;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(menuName = "Game/Item View Catalog", fileName = "ItemViewCatalog")]
    public sealed class ItemViewCatalog : ScriptableObject {
        public const string ResourceName = "ItemViewCatalog";

        [Serializable]
        public sealed class Entry {
            [SerializeField] private ItemViewId _id;
            [SerializeField] private GameObject _prefab;

            public ItemViewId Id => _id;
            public GameObject Prefab => _prefab;
        }

        [SerializeField] private Entry[] _views;

        public bool TryGetPrefab(ItemViewId id, out GameObject prefab) {
            prefab = null;
            if (id == ItemViewId.None || _views == null)
                return false;

            for (int i = 0; i < _views.Length; i++) {
                Entry entry = _views[i];
                if (entry == null || entry.Id != id || entry.Prefab == null)
                    continue;

                prefab = entry.Prefab;
                return true;
            }

            return false;
        }
    }
}
