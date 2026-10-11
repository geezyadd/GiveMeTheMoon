using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Every module the game knows: slots, stats, uninstall and the shop resolve a module id here.
    [CreateAssetMenu(menuName = "Game/Module Catalog", fileName = "ModuleCatalog_Default")]
    public sealed class ModuleCatalog : ScriptableObject {
        [SerializeField] private ModuleDefinition[] _modules = Array.Empty<ModuleDefinition>();

        public IReadOnlyList<ModuleDefinition> Modules => _modules;

        public bool TryGet(string id, out ModuleDefinition module) {
            module = null;
            if (string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < _modules.Length; i++) {
                if (_modules[i].Id != id)
                    continue;

                module = _modules[i];
                return true;
            }

            return false;
        }
    }
}
