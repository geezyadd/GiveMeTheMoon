using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // Every kind of target balloon the flight field knows; a balloon of a field resolves its kind id here.
    [CreateAssetMenu(menuName = "Game/Flight Field/Target Catalog", fileName = "FieldTargetCatalog_Default")]
    public sealed class FieldTargetCatalog : ScriptableObject {
        [SerializeField] private FieldTargetDefinition[] _targets = Array.Empty<FieldTargetDefinition>();

        public IReadOnlyList<FieldTargetDefinition> Targets => _targets;

        // For ids that come from a field: a balloon of a kind the catalog does not know is a broken field. Touches no
        // Unity object (not even the asset name), so the generation thread may call it.
        public FieldTargetDefinition GetRequired(string id) {
            if (!TryGet(id, out FieldTargetDefinition target))
                throw new KeyNotFoundException($"No target balloon '{id}' in the target catalog.");

            return target;
        }

        public bool TryGet(string id, out FieldTargetDefinition target) {
            target = null;
            if (string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < _targets.Length; i++) {
                if (_targets[i].Id != id)
                    continue;

                target = _targets[i];
                return true;
            }

            return false;
        }
    }
}
