using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Repair;
using UnityEngine;

namespace Features.ShopModule.Scripts.Configurations {
    [CreateAssetMenu(
        fileName = nameof(ShopCatalog) + "_Default",
        menuName = "Configurations/ShopModule/" + nameof(ShopCatalog))]
    public sealed class ShopCatalog : ScriptableObject {
        [SerializeField] private ModuleDefinition[] _modules = Array.Empty<ModuleDefinition>();
        [Tooltip("Repair tools, listed in the shop after the modules.")]
        [SerializeField] private RepairToolDefinition[] _tools = Array.Empty<RepairToolDefinition>();

        public IReadOnlyList<ModuleDefinition> Modules => _modules;
        public IReadOnlyList<RepairToolDefinition> Tools => _tools;
        // Shop entry indices: the modules first, then the tools.
        public int EntryCount => _modules.Length + _tools.Length;
    }
}
