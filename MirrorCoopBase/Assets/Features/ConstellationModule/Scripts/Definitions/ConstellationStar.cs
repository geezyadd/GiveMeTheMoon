using System;
using UnityEngine;

namespace Features.ConstellationModule.Scripts.Definitions {
    [Serializable]
    public sealed class ConstellationStar {
        [SerializeField] private string _name;
        [Tooltip("Right ascension, J2000, in degrees (0..360).")]
        [SerializeField] private double _rightAscensionDegrees;
        [Tooltip("Declination, J2000, in degrees (-90..90).")]
        [SerializeField] private double _declinationDegrees;
        [Tooltip("Apparent visual (V) magnitude: smaller is brighter. Sets the drawn size and brightness of the star.")]
        [SerializeField] private float _magnitude;

        public string Name => _name;
        public double RightAscensionDegrees => _rightAscensionDegrees;
        public double DeclinationDegrees => _declinationDegrees;
        public float Magnitude => _magnitude;
    }
}
