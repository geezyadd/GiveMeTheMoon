using System.Collections.Generic;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Definitions {
    // Every constellation the sky can reveal; a new one is a new ConstellationDefinition asset and an entry here.
    [CreateAssetMenu(fileName = nameof(ConstellationCatalog) + "_Default", menuName = "Configurations/Constellation/" + nameof(ConstellationCatalog))]
    public sealed class ConstellationCatalog : ScriptableObject {
        [SerializeField] private ConstellationDefinition[] _constellations;

        public IReadOnlyList<ConstellationDefinition> Constellations => _constellations;
    }
}
