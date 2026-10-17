using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Structures {
    // How the rocks of passable cells look: small and big rocks crossing the flight plane (some above or below it for
    // volume), so the ship has to fly around them. Every peer lays them out from the field seed and the field
    // position; the gap between two rocks never drops below the configured ship gap.
    [CreateAssetMenu(menuName = "Game/Flight Field/Debris Recipe", fileName = "FieldDebrisRecipe")]
    public sealed class FieldDebrisRecipe : ScriptableObject {
        [SerializeField] private FieldClusterRockOption[] _rocks = Array.Empty<FieldClusterRockOption>();
        [Tooltip("Shared rock materials, picked with equal chance per rock.")]
        [SerializeField] private Material[] _materials = Array.Empty<Material>();
        [Tooltip("Diameter of a small rock, meters, min and max.")]
        [SerializeField] private Vector2 _smallDiameterMeters = new(10f, 35f);
        [Tooltip("Diameter of a big rock, meters, min and max.")]
        [SerializeField] private Vector2 _bigDiameterMeters = new(50f, 140f);
        [Tooltip("Share of rocks drawn big.")]
        [SerializeField, Range(0f, 1f)] private float _bigShare;
        [Tooltip("A rock in the plane keeps its middle within this share of its radius from the plane, so it crosses the plane.")]
        [SerializeField, Range(0f, 1f)] private float _crossShare = 0.3f;
        [Tooltip("Share of rocks that stand above or below the plane instead, for volume.")]
        [SerializeField, Range(0f, 1f)] private float _offPlaneShare = 0.15f;
        [Tooltip("How far an off-plane rock's surface stands from the plane, meters, min and max.")]
        [SerializeField] private Vector2 _offPlaneHeightMeters = new(20f, 80f);
        [Tooltip("Random per-axis scale of every rock (non-uniform), min and max.")]
        [SerializeField] private Vector2 _axisScale = new(0.8f, 1.2f);

        public IReadOnlyList<FieldClusterRockOption> Rocks => _rocks;
        public IReadOnlyList<Material> Materials => _materials;
        public Vector2 SmallDiameterMeters => _smallDiameterMeters;
        public Vector2 BigDiameterMeters => _bigDiameterMeters;
        public float BigShare => _bigShare;
        public float CrossShare => _crossShare;
        public float OffPlaneShare => _offPlaneShare;
        public Vector2 OffPlaneHeightMeters => _offPlaneHeightMeters;
        public Vector2 AxisScale => _axisScale;
    }
}
