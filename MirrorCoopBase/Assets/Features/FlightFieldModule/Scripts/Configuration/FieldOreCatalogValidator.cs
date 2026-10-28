using System;
using System.Collections.Generic;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the ore catalog: every kind set, ids present and unique, a prefab, a size, a price, a
    // lifetime, and at least one kind a broken rock can drop.
    public sealed class FieldOreCatalogValidator : IInitializable {
        private readonly FieldOreCatalog _fieldOreCatalog;

        public FieldOreCatalogValidator(FieldOreCatalog fieldOreCatalog) =>
            _fieldOreCatalog = fieldOreCatalog;

        public void Initialize() {
            HashSet<string> ids = new(StringComparer.Ordinal);
            float totalWeight = 0f;
            for (int i = 0; i < _fieldOreCatalog.Ores.Count; i++) {
                FieldOreDefinition ore = _fieldOreCatalog.Ores[i];
                ValidateOre(ore, i, ids);
                totalWeight += ore.Weight;
            }

            if (totalWeight <= 0f)
                throw new InvalidOperationException($"{_fieldOreCatalog.name}: no ore kind has a spawn weight above 0.");
        }

        private void ValidateOre(FieldOreDefinition ore, int index, HashSet<string> ids) {
            if (ore == null)
                throw new InvalidOperationException($"{_fieldOreCatalog.name}: ore entry {index} is empty.");

            if (string.IsNullOrEmpty(ore.Id))
                throw new InvalidOperationException($"{_fieldOreCatalog.name}: ore {ore.name} has no id.");

            if (!ids.Add(ore.Id))
                throw new InvalidOperationException($"{_fieldOreCatalog.name}: ore id '{ore.Id}' is used twice (second: {ore.name}).");

            if (ore.Prefab == null)
                throw new InvalidOperationException($"{ore.name}: the ore has no prefab.");

            if (ore.RadiusMeters <= 0f || ore.Price <= 0 || ore.LifetimeSeconds <= 0f || ore.Weight < 0f)
                throw new InvalidOperationException($"{ore.name}: radius {ore.RadiusMeters}, price {ore.Price} and lifetime {ore.LifetimeSeconds} must be above 0, spawn weight {ore.Weight} not below.");
        }
    }
}
