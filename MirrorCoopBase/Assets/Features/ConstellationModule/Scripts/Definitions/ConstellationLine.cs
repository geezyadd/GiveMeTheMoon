using System;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Definitions {
    [Serializable]
    public sealed class ConstellationLine {
        [Tooltip("Index of the star the line starts at, in the constellation's star list.")]
        [SerializeField] private int _fromStar;
        [Tooltip("Index of the star the line ends at, in the constellation's star list.")]
        [SerializeField] private int _toStar;

        public int FromStar => _fromStar;
        public int ToStar => _toStar;
    }
}
