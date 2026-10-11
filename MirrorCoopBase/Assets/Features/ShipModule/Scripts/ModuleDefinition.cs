using System;
using System.Collections.Generic;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The single description of one ship module; code reads every module only through it.
    [CreateAssetMenu(menuName = "Game/Module Definition", fileName = "ModuleDefinition")]
    public sealed class ModuleDefinition : ScriptableObject {
        [Serializable]
        public sealed class StatModifierEntry {
            public ShipStatType Stat;
            public float Value;
            public ModifierType Type;
        }

        [Tooltip("Stable id stored in ship slots and sent over the network: never rename it once it is used.")]
        [SerializeField] private string _id;
        [Tooltip("The slot category this module is installed into.")]
        [SerializeField] private ShipModuleType _slotType;
        [SerializeField] private ShipItem _itemPrefab;
        [SerializeField] private GameObject _slotViewPrefab;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private long _price;
        [SerializeField] private StatModifierEntry[] _statModifiers = Array.Empty<StatModifierEntry>();
        [SerializeField] private bool _isRemovable;
        [Tooltip("When installed, the helm pilot steers the ship by hand.")]
        [SerializeField] private bool _isControlModule;

        public string Id => _id;
        public ShipModuleType SlotType => _slotType;
        public ShipItem ItemPrefab => _itemPrefab;
        public GameObject SlotViewPrefab => _slotViewPrefab;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
        public long Price => _price;
        public IReadOnlyList<StatModifierEntry> StatModifiers => _statModifiers;
        public bool IsRemovable => _isRemovable;
        public bool IsControlModule => _isControlModule;
    }
}
