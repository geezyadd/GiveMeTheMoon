using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // Every structure and every kind of debris the flight field knows; a placement resolves its structure id here, a
    // debris cell its debris id.
    [CreateAssetMenu(menuName = "Game/Flight Field/Structure Catalog", fileName = "FieldStructureCatalog_Default")]
    public sealed class FieldStructureCatalog : ScriptableObject {
        [SerializeField] private FieldStructureDefinition[] _structures = Array.Empty<FieldStructureDefinition>();
        [SerializeField] private FieldDebrisDefinition[] _debris = Array.Empty<FieldDebrisDefinition>();

        public IReadOnlyList<FieldStructureDefinition> Structures => _structures;
        public IReadOnlyList<FieldDebrisDefinition> Debris => _debris;

        // For ids that come from a field: a debris cell of a kind the catalog does not know is a broken field. Touches
        // no Unity object (not even the asset name), so the generation thread may call it.
        public FieldDebrisDefinition GetRequiredDebris(string id) {
            for (int i = 0; i < _debris.Length; i++) {
                if (_debris[i].Id == id)
                    return _debris[i];
            }

            throw new KeyNotFoundException($"No debris '{id}' in the structure catalog.");
        }

        // For ids that come from a field: a placement of a structure the catalog does not know is a broken field.
        public FieldStructureDefinition GetRequired(string id) {
            if (!TryGet(id, out FieldStructureDefinition structure))
                throw new KeyNotFoundException($"No structure '{id}' in {name}.");

            return structure;
        }

        public bool TryGet(string id, out FieldStructureDefinition structure) {
            structure = null;
            if (string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < _structures.Length; i++) {
                if (_structures[i].Id != id)
                    continue;

                structure = _structures[i];
                return true;
            }

            return false;
        }
    }
}
