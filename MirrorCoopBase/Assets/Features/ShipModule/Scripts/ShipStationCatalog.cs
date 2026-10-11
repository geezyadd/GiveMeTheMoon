using System;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    [CreateAssetMenu(menuName = "Game/Ship Station Catalog", fileName = "ShipStationCatalog")]
    public sealed class ShipStationCatalog : ScriptableObject {
        [Serializable]
        public sealed class DropEntry {
            [SerializeField] private ModuleDefinition _module;
            [SerializeField] private int _minLoop;
            [SerializeField] private int _count = 1;

            public ModuleDefinition Module => _module;
            public int MinLoop => Mathf.Max(0, _minLoop);
            public int Count => Mathf.Max(0, _count);
        }

        [SerializeField] private GameObject _padPrefab;
        [SerializeField] private GameObject _wreckPrefab;
        [SerializeField] private DropEntry[] _drops;

        public GameObject PadPrefab => _padPrefab;
        public GameObject WreckPrefab => _wreckPrefab;
        public DropEntry[] Drops => _drops;
    }
}
