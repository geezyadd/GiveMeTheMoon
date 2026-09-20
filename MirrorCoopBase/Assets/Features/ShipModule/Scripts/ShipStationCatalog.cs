using System;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(menuName = "Game/Ship Station Catalog", fileName = "ShipStationCatalog")]
    public sealed class ShipStationCatalog : ScriptableObject {
        public const string ResourceName = "ShipStationCatalog";

        [Serializable]
        public sealed class DropEntry {
            [SerializeField] private GameObject _prefab;
            [SerializeField] private int _minLoop;
            [SerializeField] private int _count = 1;

            public GameObject Prefab => _prefab;
            public int MinLoop => Mathf.Max(0, _minLoop);
            public int Count => Mathf.Max(0, _count);
        }

        [SerializeField] private GameObject _padPrefab;
        [SerializeField] private GameObject _wreckPrefab;
        [SerializeField] private GameObject _rockPrefab;
        [SerializeField] private DropEntry[] _drops;

        public GameObject PadPrefab => _padPrefab;
        public GameObject WreckPrefab => _wreckPrefab;
        public GameObject RockPrefab => _rockPrefab;
        public DropEntry[] Drops => _drops;
    }
}
