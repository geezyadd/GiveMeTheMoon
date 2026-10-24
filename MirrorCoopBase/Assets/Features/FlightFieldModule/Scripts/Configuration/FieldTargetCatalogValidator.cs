using System;
using System.Collections.Generic;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the target catalog: every kind set, ids present and unique, a prefab, a size, a reward,
    // hit points, and at least one kind the generator can pick.
    public sealed class FieldTargetCatalogValidator : IInitializable {
        private readonly FieldTargetCatalog _fieldTargetCatalog;

        public FieldTargetCatalogValidator(FieldTargetCatalog fieldTargetCatalog) =>
            _fieldTargetCatalog = fieldTargetCatalog;

        public void Initialize() {
            HashSet<string> ids = new(StringComparer.Ordinal);
            float totalWeight = 0f;
            for (int i = 0; i < _fieldTargetCatalog.Targets.Count; i++) {
                FieldTargetDefinition target = _fieldTargetCatalog.Targets[i];
                ValidateTarget(target, i, ids);
                totalWeight += target.Weight;
            }

            if (totalWeight <= 0f)
                throw new InvalidOperationException($"{_fieldTargetCatalog.name}: no target balloon has a spawn weight above 0.");
        }

        private void ValidateTarget(FieldTargetDefinition target, int index, HashSet<string> ids) {
            if (target == null)
                throw new InvalidOperationException($"{_fieldTargetCatalog.name}: target entry {index} is empty.");

            if (string.IsNullOrEmpty(target.Id))
                throw new InvalidOperationException($"{_fieldTargetCatalog.name}: target {target.name} has no id.");

            if (!ids.Add(target.Id))
                throw new InvalidOperationException($"{_fieldTargetCatalog.name}: target id '{target.Id}' is used twice (second: {target.name}).");

            if (target.Prefab == null)
                throw new InvalidOperationException($"{target.name}: the target balloon has no prefab.");

            if (target.RadiusMeters <= 0f || target.Reward <= 0 || target.HitPoints <= 0f || target.Weight < 0f)
                throw new InvalidOperationException($"{target.name}: radius {target.RadiusMeters}, reward {target.Reward} and hit points {target.HitPoints} must be above 0, spawn weight {target.Weight} not below.");
        }
    }
}
