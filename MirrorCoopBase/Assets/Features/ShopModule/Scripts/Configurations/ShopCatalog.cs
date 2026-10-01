using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Features.ShopModule.Scripts.Configurations {
    [CreateAssetMenu(
        fileName = nameof(ShopCatalog) + "_Default",
        menuName = "Configurations/ShopModule/" + nameof(ShopCatalog))]
    public sealed class ShopCatalog : ScriptableObject {
        [Serializable]
        public sealed class Entry {
            [SerializeField] private ShipItem _item;
            [SerializeField] private string _displayName;
            [SerializeField] private Sprite _icon;
            [SerializeField] private long _price;
            [SerializeField, TextArea] private string _description;

            public ShipItem Item => _item;
            public string DisplayName => _displayName;
            public Sprite Icon => _icon;
            public long Price => _price;
            public string Description => _description;
        }

        [SerializeField] private float _spawnDistance = 1.5f;
        [SerializeField] private float _spawnHeight = 1f;
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public float SpawnDistance => _spawnDistance;
        public float SpawnHeight => _spawnHeight;
        public IReadOnlyList<Entry> Entries => _entries;
    }
}
