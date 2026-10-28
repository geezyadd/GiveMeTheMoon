using System;
using Features.SonarModule.Contracts;
using Features.SonarModule.Scripts.Configurations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.SonarModule.Editor.Tests {
    // A sonar configuration with its kinds made in memory for a test, set through the serialized fields.
    public sealed class SonarTestConfiguration : IDisposable {
        public const int MARK_LIMIT = 2;
        public const float DEBRIS_CELL_METERS = 100f;
        public const int DEBRIS_MIN_ROCKS = 3;
        public const float MIN_RANGE_METERS = 300f;
        public const float MARGIN_PIXELS = 50f;
        public const float COOLDOWN_SECONDS = 2f;
        public const float WAVE_SECONDS = 1f;
        public const float ECHO_SECONDS = 4f;
        public const float KEEP_OPEN_METERS = 3f;
        public const float BIG_ROCK_MIN_DIAMETER_METERS = 40f;

        public SonarConfiguration Configuration { get; }
        public SonarContactKind StationKind { get; }
        public SonarContactKind LargeRockKind { get; }
        public SonarContactKind BigRockKind { get; }
        public SonarContactKind DebrisKind { get; }

        public SonarTestConfiguration() {
            StationKind = CreateKind("station", true);
            LargeRockKind = CreateKind("large-rock", false);
            BigRockKind = CreateKind("big-rock", false);
            DebrisKind = CreateKind("debris", false);
            Configuration = ScriptableObject.CreateInstance<SonarConfiguration>();
            SerializedObject configuration = new SerializedObject(Configuration);
            configuration.FindProperty("_markLimit").intValue = MARK_LIMIT;
            configuration.FindProperty("_debrisCellMeters").floatValue = DEBRIS_CELL_METERS;
            configuration.FindProperty("_debrisMinRocks").intValue = DEBRIS_MIN_ROCKS;
            configuration.FindProperty("_minRangeMeters").floatValue = MIN_RANGE_METERS;
            configuration.FindProperty("_markerEdgeMarginPixels").floatValue = MARGIN_PIXELS;
            configuration.FindProperty("_pingCooldownSeconds").floatValue = COOLDOWN_SECONDS;
            configuration.FindProperty("_pingWaveSeconds").floatValue = WAVE_SECONDS;
            configuration.FindProperty("_echoFadeSeconds").floatValue = ECHO_SECONDS;
            configuration.FindProperty("_consoleKeepOpenMeters").floatValue = KEEP_OPEN_METERS;
            configuration.FindProperty("_bigRockMinDiameterMeters").floatValue = BIG_ROCK_MIN_DIAMETER_METERS;
            configuration.FindProperty("_stationKind").objectReferenceValue = StationKind;
            configuration.FindProperty("_largeRockKind").objectReferenceValue = LargeRockKind;
            configuration.FindProperty("_bigRockKind").objectReferenceValue = BigRockKind;
            configuration.FindProperty("_debrisKind").objectReferenceValue = DebrisKind;
            SetKinds(configuration.FindProperty("_kinds"));
            configuration.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Dispose() {
            Object.DestroyImmediate(Configuration);
            Object.DestroyImmediate(StationKind);
            Object.DestroyImmediate(LargeRockKind);
            Object.DestroyImmediate(BigRockKind);
            Object.DestroyImmediate(DebrisKind);
        }

        private void SetKinds(SerializedProperty kinds) {
            SonarContactKind[] all = { StationKind, LargeRockKind, BigRockKind, DebrisKind };
            kinds.arraySize = all.Length;
            for (int i = 0; i < all.Length; i++)
                kinds.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
        }

        private static SonarContactKind CreateKind(string id, bool isShownBeyondRange) {
            SonarContactKind kind = ScriptableObject.CreateInstance<SonarContactKind>();
            SerializedObject serialized = new SerializedObject(kind);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_isShownBeyondRange").boolValue = isShownBeyondRange;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return kind;
        }
    }
}
