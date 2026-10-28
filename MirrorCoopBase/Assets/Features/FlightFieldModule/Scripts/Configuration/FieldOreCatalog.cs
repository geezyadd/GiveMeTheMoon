using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // Every kind of ore a broken rock can drop; a piece of the live field resolves its kind id here.
    [CreateAssetMenu(menuName = "Game/Flight Field/Ore Catalog", fileName = "FieldOreCatalog_Default")]
    public sealed class FieldOreCatalog : ScriptableObject {
        private const string UNKNOWN_KIND_FORMAT = "{0}: no ore kind '{1}'.";

        [SerializeField] private FieldOreDefinition[] _ores = Array.Empty<FieldOreDefinition>();

        public IReadOnlyList<FieldOreDefinition> Ores => _ores;

        // For ids that come from the live field: a piece of a kind the catalog does not know is a broken field.
        public FieldOreDefinition GetRequired(string id) {
            if (!TryGet(id, out FieldOreDefinition ore))
                throw new KeyNotFoundException(string.Format(UNKNOWN_KIND_FORMAT, name, id));

            return ore;
        }

        public bool TryGet(string id, out FieldOreDefinition ore) {
            ore = null;
            foreach (FieldOreDefinition candidate in _ores) {
                if (candidate.Id != id)
                    continue;

                ore = candidate;
                return true;
            }

            return false;
        }
    }
}
