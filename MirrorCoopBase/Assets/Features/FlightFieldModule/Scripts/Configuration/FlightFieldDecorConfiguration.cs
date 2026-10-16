using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
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
        [Tooltip("Distance from the route to the planet's near surface, meters, min and max: a bigger planet stands farther. Pulled in when the far edge would leave the view reach.")]
        [SerializeField] private Vector2 _planetDistance = new(2000f, 3600f);
        [Tooltip("Where in the sky a planet stands and how often: a side is picked by weight, then its elevation, so the planet stays there whatever the light does.")]
        [SerializeField] private PlanetSide[] _planetSides = Array.Empty<PlanetSide>();
        [Tooltip("Smallest angle between the course ahead (A -> B, seen from the start of the field) and the planet's middle, degrees: a planet never covers station B.")]
        [SerializeField, Range(1f, 80f)] private float _planetMinAngleFromCourse = 35f;
        [Tooltip("Smallest gap between a planet's surface and the field, meters: beside the field's width across the route axis, or above / below the decor rock layers.")]
        [SerializeField, Min(0f)] private float _planetFieldClearance = 300f;
        [Tooltip("Smallest gap between a planet's surface and the route axis A -> B, meters: both station pads stand on it, so no planet comes near them.")]
        [SerializeField, Min(0f)] private float _planetStationClearance = 1000f;
        [Tooltip("How far along the route the planet may stand from the field origin, meters, min and max.")]
        [SerializeField] private Vector2 _planetAlongRoute = new(0f, 1500f);
        [Tooltip("Largest turn of the planet away from the main light's travel direction around the vertical, degrees: the light comes from behind the viewer, so the lit side faces the ship.")]
        [SerializeField, Range(0f, 180f)] private float _planetMaxAngleFromLight = 50f;
        [Tooltip("Seeded positions tried within the picked side around the light's direction; the closest one to it is used.")]
        [SerializeField, Min(1)] private int _planetPlacementTries = 8;
        [Tooltip("The player camera's far clip plane, meters: nothing of a planet may lie beyond it.")]
        [SerializeField, Min(1f)] private float _viewFarClipPlane = 5000f;
        [Tooltip("How far inside the far clip plane the planet's far edge stays, meters.")]
        [SerializeField, Min(0f)] private float _planetFarClipMargin = 300f;
        [Tooltip("Planets picked by weight, each with its own size.")]
        [SerializeField] private PlanetDefinition[] _planets = Array.Empty<PlanetDefinition>();

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
        public IReadOnlyList<PlanetSide> PlanetSides => _planetSides;
        public float PlanetMinAngleFromCourse => _planetMinAngleFromCourse;
        public float PlanetFieldClearance => _planetFieldClearance;
        public float PlanetStationClearance => _planetStationClearance;
        public Vector2 PlanetAlongRoute => _planetAlongRoute;
        public float PlanetMaxAngleFromLight => _planetMaxAngleFromLight;
        public int PlanetPlacementTries => _planetPlacementTries;
        public float ViewFarClipPlane => _viewFarClipPlane;
        public float PlanetFarClipMargin => _planetFarClipMargin;
        public IReadOnlyList<PlanetDefinition> Planets => _planets;

        [Serializable]
        public sealed class PlanetDefinition : IFieldWeighted {
            [Tooltip("The planet prefab: a sphere of 1 m diameter, scaled to the picked diameter.")]
            [SerializeField] private GameObject _prefab;
            [Tooltip("Relative chance of this planet among all planets.")]
            [SerializeField, Min(0f)] private float _weight = 1f;
            [Tooltip("Planet diameter, meters, min and max. Shrunk (not below the min) when the planet would not fit between the clearances and the view reach; a planet that does not fit even then is not shown.")]
            [SerializeField] private Vector2 _diameter = new(700f, 1100f);

            public GameObject Prefab => _prefab;
            public float Weight => _weight;
            public Vector2 Diameter => _diameter;
        }

        [Serializable]
        public sealed class PlanetSide : IFieldWeighted {
            [Tooltip("Name shown in the inspector and in validation messages.")]
            [SerializeField] private string _name;
            [Tooltip("Relative chance of this side among all sides.")]
            [SerializeField, Min(0f)] private float _weight = 1f;
            [Tooltip("Elevation of the planet above the flight plane, degrees, min and max; negative is below the plane.")]
            [SerializeField] private Vector2 _elevationDegrees;
            [Tooltip("The elevation is mirrored below the plane with equal chance (a side near the horizon).")]
            [SerializeField] private bool _isMirroredBelowPlane;

            public string Name => _name;
            public float Weight => _weight;
            public Vector2 ElevationDegrees => _elevationDegrees;
            public bool IsMirroredBelowPlane => _isMirroredBelowPlane;
        }
    }
}
