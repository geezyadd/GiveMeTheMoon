using System.Collections.Generic;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Ore;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // What a broken rock drops: a small, a big and a giant rock by their own chance and count from the rock
    // configuration (a test one: small 35 % one piece, big 80 % one to three, giant from 150 m always three to five),
    // and the shipped catalog and configurations pass their startup checks.
    public sealed class FieldOreDropTests {
        private const float SMALL_DIAMETER = 20f;
        private const float BIG_DIAMETER = 80f;
        private const float GIANT_DIAMETER = 200f;
        private const float BELOW_SMALL_CHANCE = 0.34f;
        private const float ABOVE_SMALL_CHANCE = 0.36f;
        private const float LAST_COUNT_ROLL = 0.999f;
        private const int SEED = 5;
        private const int DROP_COUNT = 6;
        private const float SMALL_MAX_DIAMETER = 35f;
        private const float GIANT_MIN_DIAMETER = 150f;
        private const float SMALL_CHANCE = 0.35f;
        private const float BIG_CHANCE = 0.8f;
        private const int BIG_MOST = 3;
        private const int GIANT_FEWEST = 3;
        private const int GIANT_MOST = 5;

        private FieldRockDurabilityConfiguration _durability;
        private FieldOreDropRule _rule;

        [SetUp]
        public void SetUp() {
            _durability = ScriptableObject.CreateInstance<FieldRockDurabilityConfiguration>();
            SerializedObject durability = new SerializedObject(_durability);
            durability.FindProperty("_smallMaxDiameterMeters").floatValue = SMALL_MAX_DIAMETER;
            durability.FindProperty("_giantMinDiameterMeters").floatValue = GIANT_MIN_DIAMETER;
            SetDrop(durability.FindProperty("_smallRockOre"), SMALL_CHANCE, 1, 1);
            SetDrop(durability.FindProperty("_bigRockOre"), BIG_CHANCE, 1, BIG_MOST);
            SetDrop(durability.FindProperty("_giantRockOre"), 1f, GIANT_FEWEST, GIANT_MOST);
            durability.ApplyModifiedPropertiesWithoutUndo();
            _rule = new FieldOreDropRule(_durability);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_durability);

        [Test]
        public void WhenSmallRockRollsBelowItsChance_ThenItDropsOnePieceAndAboveItNone() {
            FieldRockDescription rock = CreateRock(SMALL_DIAMETER, FieldRockSize.Small);

            Assert.AreEqual(1, _rule.GetCount(rock, BELOW_SMALL_CHANCE, 0f), "A small rock below its chance drops no piece.");
            Assert.AreEqual(0, _rule.GetCount(rock, ABOVE_SMALL_CHANCE, 0f), "A small rock above its chance drops ore.");
        }

        [Test]
        public void WhenBigRockDrops_ThenItsCountSpreadsOverOneToThree() {
            FieldRockDescription rock = CreateRock(BIG_DIAMETER, FieldRockSize.Large);

            Assert.AreEqual(1, _rule.GetCount(rock, 0f, 0f));
            Assert.AreEqual(2, _rule.GetCount(rock, 0f, 0.5f));
            Assert.AreEqual(3, _rule.GetCount(rock, 0f, LAST_COUNT_ROLL));
            Assert.AreEqual(3, _rule.GetCount(rock, 0f, 1f), "A roll of 1 drops more than the most.");
            Assert.AreEqual(0, _rule.GetCount(rock, 0.9f, 0f), "A big rock above its chance drops ore.");
        }

        [Test]
        public void WhenLargeRockReachesTheGiantDiameter_ThenItDropsAsAGiant() {
            FieldRockDescription rock = CreateRock(GIANT_DIAMETER, FieldRockSize.Large);

            Assert.AreEqual(3, _rule.GetCount(rock, LAST_COUNT_ROLL, 0f), "A giant does not always drop its fewest pieces.");
            Assert.AreEqual(5, _rule.GetCount(rock, LAST_COUNT_ROLL, LAST_COUNT_ROLL));
        }

        [Test]
        public void WhenShippedOreDataIsRead_ThenItPassesItsStartupChecks() {
            FieldOreCatalog catalog = AddressableConfigurationLoader.Load<FieldOreCatalog>(Address.Configurations.FieldOreCatalog_Default);
            FieldOreConfiguration configuration = AddressableConfigurationLoader.Load<FieldOreConfiguration>(Address.Configurations.FieldOreConfiguration_Default);
            FieldRockDurabilityConfiguration durability = AddressableConfigurationLoader.Load<FieldRockDurabilityConfiguration>(Address.Configurations.FieldRockDurabilityConfiguration_Default);

            Assert.DoesNotThrow(() => new FieldOreCatalogValidator(catalog).Initialize());
            Assert.DoesNotThrow(() => new FieldOreConfigurationValidator(configuration, catalog).Initialize());
            Assert.DoesNotThrow(() => new FieldRockDurabilityConfigurationValidator(durability).Initialize());
        }

        [Test]
        public void WhenShippedOrePrefabsAreRead_ThenEachIsAnOffTriggerOnTheFieldRockLayer() {
            FieldOreCatalog catalog = AddressableConfigurationLoader.Load<FieldOreCatalog>(Address.Configurations.FieldOreCatalog_Default);
            int fieldRockLayer = LayerMask.NameToLayer("FieldRock");
            foreach (FieldOreDefinition ore in catalog.Ores) {
                Collider[] colliders = ore.Prefab.GetComponentsInChildren<Collider>(true);
                Assert.AreEqual(1, colliders.Length, $"{ore.name}: the ore needs exactly one collider, its trigger.");
                Assert.IsTrue(colliders[0].isTrigger && !colliders[0].enabled, $"{ore.name}: the collider must be a trigger, off until bound.");
                Assert.AreEqual(fieldRockLayer, ore.Prefab.gameObject.layer, $"{ore.name}: the ore is not on the FieldRock layer.");
            }
        }

        [Test]
        public void WhenRockDropsOre_ThenEveryPieceStartsNearItAndDriftsAway() {
            FieldOreTestRig rig = new(null);
            FieldOreDropService service = new(new FieldOreDropRule(_durability), rig.Spawner, rig.Catalog, rig.Configuration);
            Vector3 center = new(10f, 5f, 40f);

            int dropped = service.ServerDropAt(center, 2f, DROP_COUNT, new FieldRandomFactory().Create(SEED));

            Assert.AreEqual(DROP_COUNT, dropped);
            Assert.AreEqual(DROP_COUNT, rig.Ores.Count);
            List<FieldOreDescription> pieces = new();
            rig.Query.FindAll(pieces);
            rig.Clock.Now = 5d;
            for (int i = 0; i < pieces.Count; i++) {
                Assert.LessOrEqual(Vector3.Distance(pieces[i].Center, center), 2f + 0.001f, "A piece starts outside the scatter radius.");
                rig.Query.TryGetOre(pieces[i].Id, out FieldOreDescription later);
                Assert.Greater(Vector3.Distance(later.Center, center), Vector3.Distance(pieces[i].Center, center), "A piece does not drift away from the break.");
            }
        }

        private static void SetDrop(SerializedProperty drop, float chance, int fewest, int most) {
            drop.FindPropertyRelative("_chance").floatValue = chance;
            drop.FindPropertyRelative("_minCount").intValue = fewest;
            drop.FindPropertyRelative("_maxCount").intValue = most;
        }

        private static FieldRockDescription CreateRock(float diameter, FieldRockSize size) =>
            new(FieldRockId.CreateDebris(Vector2Int.zero, 0), "debris", Vector3.zero, diameter, size, true);
    }
}
