using System.Collections.Generic;
using Features.ConstellationModule.Data;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Definitions {
    // Every constellation the sky can reveal; a new one is a new ConstellationDefinition asset and an entry here.
    [CreateAssetMenu(fileName = nameof(ConstellationCatalog) + "_Default", menuName = "Configurations/Constellation/" + nameof(ConstellationCatalog))]
    public sealed class ConstellationCatalog : ScriptableObject {
        [SerializeField] private ConstellationDefinition[] _constellations;

        public IReadOnlyList<ConstellationDefinition> Constellations => _constellations;

        public bool TryFindIndex(string id, out int index) {
            index = ConstellationRevealModel.NO_CONSTELLATION;
            for (int i = 0; i < _constellations.Length; i++) {
                if (_constellations[i].Id != id)
                    continue;

                index = i;
                return true;
            }

            return false;
        }
    }
}
