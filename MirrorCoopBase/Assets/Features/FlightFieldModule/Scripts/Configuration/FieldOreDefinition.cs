using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Ore;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // One kind of ore a broken rock can drop: how it looks, what it pays when it reaches the ship, how long it lasts
    // and how often it drops against the other kinds.
    [CreateAssetMenu(menuName = "Game/Flight Field/Ore Definition", fileName = "FieldOre_")]
    public sealed class FieldOreDefinition : ScriptableObject, IFieldWeighted {
        [Tooltip("Stable id used on the network; never rename it once used.")]
        [SerializeField] private string _id;
        [Tooltip("The piece as every peer shows it: its root is the trigger a rope strikes.")]
        [SerializeField] private FieldOreView _prefab;
        [Tooltip("Money the crew gets when the piece reaches the ship.")]
        [SerializeField, Min(0)] private int _price;
        [Tooltip("Seconds from the drop until the piece is gone; the last seconds of it it fades out.")]
        [SerializeField, Min(0f)] private float _lifetimeSeconds;
        [Tooltip("How often this kind drops against the other kinds of the catalog.")]
        [SerializeField, Min(0f)] private float _spawnWeight;
        [Tooltip("The radius of the piece's sphere, meters: where the hull catches it and a rope hit counts.")]
        [SerializeField, Min(0f)] private float _radiusMeters;

        public string Id => _id;
        public FieldOreView Prefab => _prefab;
        public int Price => _price;
        public float LifetimeSeconds => _lifetimeSeconds;
        public float RadiusMeters => _radiusMeters;
        public float Weight => _spawnWeight;
    }
}
