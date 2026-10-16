using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // Tuning of the visual-only decor of the flight field: the sparse rock layers above and below the flight plane and
    // the distant planet. Every peer builds it locally from the field seed; nothing here has a collider or is synced.
    [CreateAssetMenu(menuName = "Game/Flight Field/Decor Configuration", fileName = "FlightFieldDecorConfiguration_Default")]
    public sealed class FlightFieldDecorConfiguration : ScriptableObject {
        [Header("Rock layers")]
        [Tooltip("Height of the upper layer above the flight plane, meters, min and max. The lower layer mirrors it below the plane.")]
        [SerializeField] private Vector2 _layerHeight = new(150f, 600f);
        [Tooltip("Rocks per square kilometer in each layer.")]
        [SerializeField, Min(0f)] private float _rocksPerSquareKilometer = 40f;
        [Tooltip("Rock size, meters, min and max.")]
        [SerializeField] private Vector2 _rockSize = new(8f, 40f);
        [Tooltip("Side of one decor chunk, meters.")]
        [SerializeField, Min(10f)] private float _chunkSize = 250f;
        [Tooltip("Chunks whose middle is within this distance of the ship (along the flight plane), meters, stand; farther ones are released.")]
        [SerializeField, Min(10f)] private float _viewDistance = 1200f;
        [Tooltip("Chunks built per frame.")]
        [SerializeField, Min(1)] private int _chunksBuiltPerFrame = 2;
        [Tooltip("Which rock prefabs a decor rock uses and their weights (the recipe's sizes and materials are not used).")]
        [SerializeField] private FieldClusterRecipe _rockRecipe;
        [Tooltip("Rock materials from the nearest height of a layer to the farthest one: the darkening with distance.")]
        [SerializeField] private Material[] _materialsByDistance = Array.Empty<Material>();

        [Header("Planet")]
        [Tooltip("Chance that a flight shows a planet.")]
        [SerializeField, Range(0f, 1f)] private float _planetChance = 0.4f;
        [Tooltip("Planets per flight at most.")]
        [SerializeField, Min(0)] private int _maxPlanetsPerFlight = 1;
        [Tooltip("Distance from the route to the planet's middle, meters, min and max. The far edge must stay inside the player camera's far clip plane (5000 m).")]
        [SerializeField] private Vector2 _planetDistance = new(2000f, 3600f);
        [Tooltip("Angle of the planet above or below the flight plane, degrees, min and max.")]
        [SerializeField] private Vector2 _planetElevationDegrees = new(15f, 55f);
        [Tooltip("How far along the route the planet may stand from the field origin, meters, min and max.")]
        [SerializeField] private Vector2 _planetAlongRoute = new(0f, 1500f);
        [Tooltip("Largest angle between the direction to the planet and the main light's travel direction, degrees: the light comes from behind the viewer, so the lit side faces the ship.")]
        [SerializeField, Range(0f, 180f)] private float _planetMaxAngleFromLight = 50f;
        [Tooltip("Seeded positions tried around the light's direction; the closest one to it is used.")]
        [SerializeField, Min(1)] private int _planetPlacementTries = 8;
        [Tooltip("Planet diameter, meters, min and max.")]
        [SerializeField] private Vector2 _planetDiameter = new(500f, 1100f);
        [Tooltip("Planet prefabs picked with equal chance.")]
        [SerializeField] private GameObject[] _planetPrefabs = Array.Empty<GameObject>();

        public Vector2 LayerHeight => _layerHeight;
        public float RocksPerSquareKilometer => _rocksPerSquareKilometer;
        public Vector2 RockSize => _rockSize;
        public float ChunkSize => _chunkSize;
        public float ViewDistance => _viewDistance;
        public int ChunksBuiltPerFrame => _chunksBuiltPerFrame;
        public FieldClusterRecipe RockRecipe => _rockRecipe;
        public IReadOnlyList<Material> MaterialsByDistance => _materialsByDistance;
        public float PlanetChance => _planetChance;
        public int MaxPlanetsPerFlight => _maxPlanetsPerFlight;
        public Vector2 PlanetDistance => _planetDistance;
        public Vector2 PlanetElevationDegrees => _planetElevationDegrees;
        public Vector2 PlanetAlongRoute => _planetAlongRoute;
        public float PlanetMaxAngleFromLight => _planetMaxAngleFromLight;
        public int PlanetPlacementTries => _planetPlacementTries;
        public Vector2 PlanetDiameter => _planetDiameter;
        public IReadOnlyList<GameObject> PlanetPrefabs => _planetPrefabs;
    }
}
