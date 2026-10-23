using System;
using Features.FlightFieldModule.Scripts.Generation;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // One size class of the rocks an obstacle field lays out on its debris grid (small, medium), with its relative
    // chance among the classes of the profile.
    [Serializable]
    public sealed class FieldRockSizeClass : IFieldWeighted {
        [Tooltip("Shown in the inspector and in validation messages.")]
        [SerializeField] private string _name;
        [Tooltip("Rock diameter, meters, min and max.")]
        [SerializeField] private Vector2 _diameterMeters = new(10f, 30f);
        [SerializeField, Min(0f)] private float _weight = 1f;

        public string Name => _name;
        public Vector2 DiameterMeters => _diameterMeters;
        public float Weight => _weight;
    }
}
