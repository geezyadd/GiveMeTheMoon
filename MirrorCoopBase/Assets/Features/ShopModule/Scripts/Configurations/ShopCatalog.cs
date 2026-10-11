using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Features.ShopModule.Scripts.Configurations {
    [CreateAssetMenu(
        fileName = nameof(ShopCatalog) + "_Default",
        menuName = "Configurations/ShopModule/" + nameof(ShopCatalog))]
    public sealed class ShopCatalog : ScriptableObject {
        [SerializeField] private ModuleDefinition[] _modules = Array.Empty<ModuleDefinition>();

        public IReadOnlyList<ModuleDefinition> Modules => _modules;
    }
}
