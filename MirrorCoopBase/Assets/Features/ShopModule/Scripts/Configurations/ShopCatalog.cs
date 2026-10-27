using System;
using System.Collections.Generic;
using Features.HandWeaponModule.Scripts;
using Features.ShipDamageModule.Scripts.Repair;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Features.ShopModule.Scripts.Configurations {
    [CreateAssetMenu(
        fileName = nameof(ShopCatalog) + "_Default",
        menuName = "Configurations/ShopModule/" + nameof(ShopCatalog))]
    public sealed class ShopCatalog : ScriptableObject {
        [SerializeField] private ModuleDefinition[] _modules = Array.Empty<ModuleDefinition>();
        [Tooltip("Repair tools, listed in the shop after the modules.")]
        [SerializeField] private RepairToolDefinition[] _tools = Array.Empty<RepairToolDefinition>();
        [Tooltip("Weapons carried in the hand, listed in the shop after the tools.")]
        [SerializeField] private HandWeaponDefinition[] _handWeapons = Array.Empty<HandWeaponDefinition>();

        public IReadOnlyList<ModuleDefinition> Modules => _modules;
        public IReadOnlyList<RepairToolDefinition> Tools => _tools;
        public IReadOnlyList<HandWeaponDefinition> HandWeapons => _handWeapons;
        // Shop entry indices: the modules first, then the tools, then the hand weapons.
        public int EntryCount => _modules.Length + _tools.Length + _handWeapons.Length;
    }
}
