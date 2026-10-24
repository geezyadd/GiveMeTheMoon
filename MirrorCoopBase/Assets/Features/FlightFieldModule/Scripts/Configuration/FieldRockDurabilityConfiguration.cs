using Features.FlightFieldModule.Scripts.Rocks;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // How the rocks of the flight field break: which count as small (the rest and every rock of a recipe marked
    // always large count as large) and what a break looks like. Whether a rock breaks at all is its recipe's flag.
    [CreateAssetMenu(menuName = "Game/Flight Field/Rock Durability Configuration", fileName = "FieldRockDurabilityConfiguration_Default")]
    public sealed class FieldRockDurabilityConfiguration : ScriptableObject {
        [Tooltip("A rock up to this diameter, meters, is small; a bigger one is large.")]
        [SerializeField, Min(0f)] private float _smallMaxDiameterMeters = 35f;
        [Tooltip("Played where a rock breaks, on every peer that has the rock built; authored for a rock of 1 m and scaled by the rock's diameter.")]
        [SerializeField] private FieldRockBreakEffect _breakEffect;

        public float SmallMaxDiameterMeters => _smallMaxDiameterMeters;
        public FieldRockBreakEffect BreakEffect => _breakEffect;
    }
}
