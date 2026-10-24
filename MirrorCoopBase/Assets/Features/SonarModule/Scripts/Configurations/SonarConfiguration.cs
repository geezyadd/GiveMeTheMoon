using System;
using System.Collections.Generic;
using Features.SonarModule.Contracts;
using UnityEngine;

namespace Features.SonarModule.Scripts.Configurations {
    // Every number of the navigator's sonar: the ping game, the marks, the console and the contact sources.
    [CreateAssetMenu(fileName = nameof(SonarConfiguration) + "_Default", menuName = "Configurations/Sonar/" + nameof(SonarConfiguration))]
    public sealed class SonarConfiguration : ScriptableObject {
        [Header("Scope")]
        [Tooltip("The range when the ship's SonarRange stat is lower, in metres.")]
        [SerializeField] private float _minRangeMeters = 300f;
        [SerializeField] private int _rangeRingCount = 3;

        [Header("Ping")]
        [SerializeField] private float _pingCooldownSeconds = 2.5f;
        [Tooltip("How long the wave takes from the ship to the edge of the scope.")]
        [SerializeField] private float _pingWaveSeconds = 1.2f;
        [Tooltip("How long a contact the wave lit stays on the scope while it fades.")]
        [SerializeField] private float _echoFadeSeconds = 4f;

        [Header("Marks")]
        [Tooltip("Marks the crew may have at once.")]
        [SerializeField] private int _markLimit = 3;
        [Tooltip("How often the server drops marks that ran out or whose object broke.")]
        [SerializeField] private float _serverCheckSeconds = 0.25f;
        [Tooltip("How far a world marker keeps from the screen edge while it points at a contact off screen, in pixels.")]
        [SerializeField] private float _markerEdgeMarginPixels = 56f;

        [Header("Console")]
        [Tooltip("The navigator leaves the console once their body is farther than this from it, in metres.")]
        [SerializeField] private float _consoleKeepOpenMeters = 3f;

        [Header("Contacts")]
        [SerializeField] private SonarContactKind[] _kinds = Array.Empty<SonarContactKind>();
        [SerializeField] private SonarContactKind _stationKind;
        [SerializeField] private SonarContactKind _largeRockKind;
        [SerializeField] private SonarContactKind _debrisKind;
        [Tooltip("Small rocks are counted in square cells of this size; a cell with enough of them is one debris contact.")]
        [SerializeField] private float _debrisCellMeters = 120f;
        [SerializeField] private int _debrisMinRocks = 6;
        [Tooltip("A debris cell reaches this far above and below the world's zero height when the server looks it up again.")]
        [SerializeField] private float _debrisCellHalfHeightMeters = 2000f;

        public float MinRangeMeters => _minRangeMeters;
        public int RangeRingCount => _rangeRingCount;
        public float PingCooldownSeconds => _pingCooldownSeconds;
        public float PingWaveSeconds => _pingWaveSeconds;
        public float EchoFadeSeconds => _echoFadeSeconds;
        public int MarkLimit => _markLimit;
        public float ServerCheckSeconds => _serverCheckSeconds;
        public float MarkerEdgeMarginPixels => _markerEdgeMarginPixels;
        public float ConsoleKeepOpenMeters => _consoleKeepOpenMeters;
        public IReadOnlyList<SonarContactKind> Kinds => _kinds;
        public SonarContactKind StationKind => _stationKind;
        public SonarContactKind LargeRockKind => _largeRockKind;
        public SonarContactKind DebrisKind => _debrisKind;
        public float DebrisCellMeters => _debrisCellMeters;
        public int DebrisMinRocks => _debrisMinRocks;
        public float DebrisCellHalfHeightMeters => _debrisCellHalfHeightMeters;
    }
}
