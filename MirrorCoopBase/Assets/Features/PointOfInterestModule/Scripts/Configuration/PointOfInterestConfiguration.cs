using System;
using System.Collections.Generic;
using Features.SonarModule.Contracts;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Configuration {
    // What a point of interest may hold and how the server reads the ship against it. Where the points stand is the
    // flight field's (FlightFieldConfiguration, its own generation step).
    [CreateAssetMenu(fileName = nameof(PointOfInterestConfiguration), menuName = "Configurations/Points of interest/" + nameof(PointOfInterestConfiguration))]
    public sealed class PointOfInterestConfiguration : ScriptableObject {
        [Tooltip("Every content a point may get, picked by weight.")]
        [SerializeField] private PointOfInterestContentDefinition[] _contents = Array.Empty<PointOfInterestContentDefinition>();
        [Tooltip("The zone round a point: the ship's middle entering it starts the content, once.")]
        [SerializeField, Range(20f, 300f)] private float _zoneRadiusMeters = 100f;
        [Tooltip("How far the hull reaches from the ship's middle when it rams a reward container.")]
        [SerializeField, Min(0f)] private float _shipRamReachMeters = 9f;
        [Tooltip("A reported hit may lie this far outside the container (the client shows it a moment behind).")]
        [SerializeField, Min(0f)] private float _hitToleranceMeters = 3f;
        [SerializeField] private SonarContactKind _sonarKind;
        [Header("Debug")]
        [Tooltip("\"Move the ship to the nearest point of interest\" puts the ship this far outside the zone, facing the point.")]
        [SerializeField, Min(0f)] private float _debugApproachMeters = 25f;

        public IReadOnlyList<PointOfInterestContentDefinition> Contents => _contents;
        public float ZoneRadiusMeters => _zoneRadiusMeters;
        public float ShipRamReachMeters => _shipRamReachMeters;
        public float HitToleranceMeters => _hitToleranceMeters;
        public SonarContactKind SonarKind => _sonarKind;
        public float DebugApproachMeters => _debugApproachMeters;
    }
}
