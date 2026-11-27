using System.Collections.Generic;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Definitions {
    // One constellation as data: its stars at their catalogue positions and the lines of its usual figure.
    [CreateAssetMenu(fileName = nameof(ConstellationDefinition), menuName = "Configurations/Constellation/" + nameof(ConstellationDefinition))]
    public sealed class ConstellationDefinition : ScriptableObject {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private ConstellationStar[] _stars;
        [SerializeField] private ConstellationLine[] _lines;

        public string Id => _id;
        public string DisplayName => _displayName;
        public IReadOnlyList<ConstellationStar> Stars => _stars;
        public IReadOnlyList<ConstellationLine> Lines => _lines;
    }
}
