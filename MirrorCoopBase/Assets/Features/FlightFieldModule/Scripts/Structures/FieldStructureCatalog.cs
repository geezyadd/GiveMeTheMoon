using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // Every structure the flight field knows; a placement resolves its structure id here.
    [CreateAssetMenu(menuName = "Game/Flight Field/Structure Catalog", fileName = "FieldStructureCatalog_Default")]
    public sealed class FieldStructureCatalog : ScriptableObject {
        [SerializeField] private FieldStructureDefinition[] _structures = Array.Empty<FieldStructureDefinition>();

        public IReadOnlyList<FieldStructureDefinition> Structures => _structures;

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
