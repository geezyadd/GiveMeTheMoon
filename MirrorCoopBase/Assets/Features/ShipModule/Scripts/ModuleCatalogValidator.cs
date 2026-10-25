using System;
using System.Collections.Generic;
using Zenject;

namespace Features.ShipModule.Scripts {
    // The one startup check of the module catalog: every entry set, every id present and unique, every modifier
    // names a stat.
    public sealed class ModuleCatalogValidator : IInitializable {
        private readonly ModuleCatalog _moduleCatalog;

        public ModuleCatalogValidator(ModuleCatalog moduleCatalog) =>
            _moduleCatalog = moduleCatalog;

        public void Initialize() {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _moduleCatalog.Modules.Count; i++)
                ValidateModule(_moduleCatalog.Modules[i], i, ids);
        }

        private void ValidateModule(ModuleDefinition module, int index, HashSet<string> ids) {
            if (module == null)
                throw new InvalidOperationException($"{_moduleCatalog.name}: module entry {index} is empty.");

            if (string.IsNullOrEmpty(module.Id))
                throw new InvalidOperationException($"{_moduleCatalog.name}: module {module.name} has no id.");

            if (ids.Add(module.Id) == false)
                throw new InvalidOperationException($"{_moduleCatalog.name}: module id '{module.Id}' is used twice (second: {module.name}).");

            for (int i = 0; i < module.StatModifiers.Count; i++)
                ValidateModifier(module, module.StatModifiers[i]);
        }

        private void ValidateModifier(ModuleDefinition module, ModuleDefinition.StatModifierEntry modifier) {
            if (modifier.Stat == ShipStatType.None)
                throw new InvalidOperationException($"{_moduleCatalog.name}: module {module.Id} has a stat modifier without a stat.");

            if (Enum.IsDefined(typeof(ShipStatType), modifier.Stat) == false)
                throw new InvalidOperationException($"{_moduleCatalog.name}: module {module.Id} has a modifier on the unknown stat {(int)modifier.Stat}.");
        }
    }
}
