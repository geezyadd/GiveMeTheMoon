using System;
using Features.ShipModule.Scripts.Throttle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.ShipModule.Scripts.Editor.Tests {
    // A throttle configuration made in memory for a test: Stop 0, Slow 0.3, Half 0.6, Full 1, set through the
    // serialized fields.
    public sealed class ShipThrottleTestConfiguration : IDisposable {
        public const float SLOW = 0.3f;
        public const float HALF = 0.6f;
        public const float LAUNCH_MARK = 0.15f;
        public const float PER_LOOK_UNIT = 0.01f;
        public const float SMOOTH_SECONDS = 0.1f;
        public const float MAX_SPEED = 2f;
        public const float PULL_RADIUS = 0.06f;
        public const float PULL_STRENGTH = 0.6f;
        public const float CLICK_RADIUS = 0.015f;
        public const float SEND_INTERVAL_SECONDS = 0.05f;
        public const float TAKE_TIMEOUT_SECONDS = 1f;

        private static readonly string[] _detentNames = { "Stop", "Slow", "Half", "Full" };
        private static readonly float[] _detentPositions = { 0f, SLOW, HALF, 1f };

        public ShipThrottleConfiguration Configuration { get; }

        public ShipThrottleTestConfiguration() {
            Configuration = ScriptableObject.CreateInstance<ShipThrottleConfiguration>();
            SerializedObject configuration = new SerializedObject(Configuration);
            SerializedProperty detents = configuration.FindProperty("_detents");
            detents.arraySize = _detentNames.Length;
            for (int i = 0; i < _detentNames.Length; i++) {
                SerializedProperty detent = detents.GetArrayElementAtIndex(i);
                detent.FindPropertyRelative(nameof(ShipThrottleConfiguration.Detent.Position)).floatValue = _detentPositions[i];
                detent.FindPropertyRelative(nameof(ShipThrottleConfiguration.Detent.Name)).stringValue = _detentNames[i];
            }

            configuration.FindProperty("_launchMark").floatValue = LAUNCH_MARK;
            configuration.FindProperty("_throttlePerLookUnit").floatValue = PER_LOOK_UNIT;
            configuration.FindProperty("_handleSmoothSeconds").floatValue = SMOOTH_SECONDS;
            configuration.FindProperty("_handleMaxSpeed").floatValue = MAX_SPEED;
            configuration.FindProperty("_detentPullRadius").floatValue = PULL_RADIUS;
            configuration.FindProperty("_detentPullStrength").floatValue = PULL_STRENGTH;
            configuration.FindProperty("_detentClickRadius").floatValue = CLICK_RADIUS;
            configuration.FindProperty("_sendIntervalSeconds").floatValue = SEND_INTERVAL_SECONDS;
            configuration.FindProperty("_takeTimeoutSeconds").floatValue = TAKE_TIMEOUT_SECONDS;
            configuration.ApplyModifiedPropertiesWithoutUndo();
        }

        public ShipThrottleHandle CreateHandle() =>
            new ShipThrottleHandle(Configuration, new ShipThrottleDetentRule(Configuration));

        public void Dispose() =>
            Object.DestroyImmediate(Configuration);
    }
}
