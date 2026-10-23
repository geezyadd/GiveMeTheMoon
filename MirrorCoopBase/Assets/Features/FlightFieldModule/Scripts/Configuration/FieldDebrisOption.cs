using System;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // A kind of small debris a profile may scatter over passable cells, with its relative chance.
    [Serializable]
    public sealed class FieldDebrisOption : IFieldWeighted {
        [SerializeField] private FieldDebrisDefinition _debris;
        [SerializeField, Min(0f)] private float _weight = 1f;

        public FieldDebrisDefinition Debris => _debris;
        public float Weight => _weight;
    }
}
