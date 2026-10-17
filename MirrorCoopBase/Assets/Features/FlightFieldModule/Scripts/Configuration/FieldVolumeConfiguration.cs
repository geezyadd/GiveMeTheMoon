using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The visual-only volume of big rocks above and below the flight plane, in height bands (near, far): every peer
    // lays it out per decor chunk from the field seed; no colliders, not in the matrix, not synced. How many rocks
    // stand follows the field's ridges (denser over ridges, sparser over glades, never empty); rocks of one band keep
    // a gap between them, and no rock comes closer to the flight plane than the clearance.
    [CreateAssetMenu(menuName = "Game/Flight Field/Volume Configuration", fileName = "FieldVolumeConfiguration_Default")]
    public sealed class FieldVolumeConfiguration : ScriptableObject {
        [Serializable]
        public sealed class Band {
            [Tooltip("Heights of the band above the flight plane, meters, min and max; mirrored below it. A rock stays inside its band.")]
            [SerializeField] private Vector2 _heightMeters = new(40f, 300f);
            [Tooltip("Spacing of the grid the rocks are drawn on, meters, along the plane and in height.")]
            [SerializeField, Min(1f)] private float _spacingMeters = 120f;
            [Tooltip("Rock diameter, meters, min and max; over ridges the rocks are bigger.")]
            [SerializeField] private Vector2 _diameterMeters = new(50f, 160f);
            [Tooltip("The least gap between two rocks of the band, meters: they never fuse.")]
            [SerializeField, Min(0f)] private float _gapMeters = 30f;
            [Tooltip("Share of grid points that become rocks over a glade (density 0) and over a ridge (density 1).")]
            [SerializeField] private Vector2 _presence = new(0.25f, 0.8f);
            [Tooltip("Materials of the band's rocks, picked with equal chance.")]
            [SerializeField] private Material[] _materials = Array.Empty<Material>();

            public Vector2 HeightMeters => _heightMeters;
            public float SpacingMeters => _spacingMeters;
            public Vector2 DiameterMeters => _diameterMeters;
            public float GapMeters => _gapMeters;
            public Vector2 Presence => _presence;
            public IReadOnlyList<Material> Materials => _materials;
        }

        [SerializeField] private Band[] _bands = Array.Empty<Band>();
        [Tooltip("Rock prefabs of the volume and their weights.")]
        [SerializeField] private FieldClusterRockOption[] _rocks = Array.Empty<FieldClusterRockOption>();
        [Tooltip("Random per-axis scale of a rock (non-uniform), min and max.")]
        [SerializeField] private Vector2 _axisScale = new(0.8f, 1.2f);
        [Tooltip("No rock comes closer to the flight plane than this, meters: nothing touches the ship or blocks the view ahead at eye height.")]
        [SerializeField, Min(0f)] private float _planeClearanceMeters = 30f;
        [Tooltip("No rock stands below the plane within this distance of the start and the end of the main route, meters: the ship climbs from and sinks to the stations there.")]
        [SerializeField, Min(0f)] private float _stationClearanceMeters = 600f;

        public IReadOnlyList<Band> Bands => _bands;
        public IReadOnlyList<FieldClusterRockOption> Rocks => _rocks;
        public Vector2 AxisScale => _axisScale;
        public float PlaneClearanceMeters => _planeClearanceMeters;
        public float StationClearanceMeters => _stationClearanceMeters;
        // The highest any band reaches above the plane (and, mirrored, below it): planets keep clear of it.
        public float TopMeters {
            get {
                float top = 0f;
                foreach (Band band in _bands)
                    top = Mathf.Max(top, band.HeightMeters.y);

                return top;
            }
        }
    }
}
