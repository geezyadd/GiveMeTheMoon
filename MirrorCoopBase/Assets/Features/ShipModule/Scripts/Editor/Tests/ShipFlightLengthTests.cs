using System;
using Features.ShipModule.Scripts.Debug;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipFlightLengthTests {
        private const string RUN_CONFIG_PATH = "Assets/Features/ShipModule/GameResources/Configurations/ShipRunConfig_Default.asset";
        private const float TOLERANCE = 0.001f;
        private const string NO_CAP_JSON = "{\"_firstFlightSeconds\":120,\"_perLoopFlightSeconds\":120,\"_maxFlightSeconds\":0}";
        private const string CAPPED_JSON = "{\"_firstFlightSeconds\":120,\"_perLoopFlightSeconds\":120,\"_maxFlightSeconds\":300}";
        private const string ZERO_FIRST_JSON = "{\"_firstFlightSeconds\":0,\"_perLoopFlightSeconds\":120,\"_maxFlightSeconds\":0}";
        private const string ZERO_PER_LOOP_JSON = "{\"_firstFlightSeconds\":120,\"_perLoopFlightSeconds\":0,\"_maxFlightSeconds\":0}";
        private const string CAP_BELOW_FIRST_JSON = "{\"_firstFlightSeconds\":120,\"_perLoopFlightSeconds\":120,\"_maxFlightSeconds\":60}";

        [TestCase(0, 120f)]
        [TestCase(1, 240f)]
        [TestCase(2, 360f)]
        [TestCase(9, 1200f)]
        public void WhenNoCap_ThenFlightSecondsGrowByThePerLoopSeconds(int loop, float expectedSeconds) =>
            WithConfig(NO_CAP_JSON, config => Assert.AreEqual(expectedSeconds, new ShipFlightStatService(new ShipRunConfigFlightLengthSource(config)).EvaluateRouteWork(loop), TOLERANCE));

        [TestCase(0, 120f)]
        [TestCase(1, 240f)]
        [TestCase(2, 300f)]
        [TestCase(9, 300f)]
        public void WhenCapped_ThenFlightSecondsStopAtTheCap(int loop, float expectedSeconds) =>
            WithConfig(CAPPED_JSON, config => Assert.AreEqual(expectedSeconds, new ShipFlightStatService(new ShipRunConfigFlightLengthSource(config)).EvaluateRouteWork(loop), TOLERANCE));

        [Test]
        public void WhenDebugFlightSecondsAreSet_ThenEveryLoopUsesThemUntilCleared() =>
            WithConfig(NO_CAP_JSON, config => {
                ShipFlightLengthDebugModel debugModel = new();
                ShipFlightStatService service = new(new ShipDebugFlightLengthSource(new ShipRunConfigFlightLengthSource(config), debugModel));
                debugModel.DebugUseFlightSeconds(20f);
                Assert.AreEqual(20f, service.EvaluateRouteWork(4), TOLERANCE);
                debugModel.DebugClearFlightSeconds();
                Assert.AreEqual(600f, service.EvaluateRouteWork(4), TOLERANCE);
            });

        [Test]
        public void WhenDefaultAssetValidated_ThenItPasses() =>
            Assert.DoesNotThrow(new ShipRunConfigValidator(AssetDatabase.LoadAssetAtPath<ShipRunConfig>(RUN_CONFIG_PATH)).Initialize);

        [TestCase(ZERO_FIRST_JSON)]
        [TestCase(ZERO_PER_LOOP_JSON)]
        [TestCase(CAP_BELOW_FIRST_JSON)]
        public void WhenFlightLengthIsBroken_ThenValidationThrows(string json) =>
            WithConfig(json, config => Assert.Throws<InvalidOperationException>(new ShipRunConfigValidator(config).Initialize));

        private static void WithConfig(string json, Action<ShipRunConfig> check) {
            ShipRunConfig config = ScriptableObject.CreateInstance<ShipRunConfig>();
            try {
                JsonUtility.FromJsonOverwrite(json, config);
                check(config);
            }
            finally {
                Object.DestroyImmediate(config);
            }
        }
    }
}
