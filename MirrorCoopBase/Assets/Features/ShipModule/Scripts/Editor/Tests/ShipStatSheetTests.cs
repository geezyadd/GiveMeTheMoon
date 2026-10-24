using Features.ShipModule.Scripts.Generated;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipStatSheetTests {
        private const string STATS_CONFIGURATION_PATH =
            "Assets/Features/ShipModule/GameResources/Configurations/ShipAccumulativeStatsConfiguration_Default.asset";
        private const float TOLERANCE = 0.0001f;
        private const string LEFT_ENGINE_SOCKET = "engine-left";
        private const string RIGHT_ENGINE_SOCKET = "engine-right";

        // The values ShipModules.ApplyDefaultStats had in code before they moved to the configuration; Armor is a
        // damage reduction since the hull got its own Hull stat, so it starts at 0.
        private const float THRUST_MAX = 999f;
        private const float THRUST = 0f;
        private const float FLIGHT_SPEED_MAX = 99f;
        private const float FLIGHT_SPEED = 1f;
        private const float DODGE_RANGE_MAX = 20f;
        private const float DODGE_RANGE = 1f;
        private const float HANDLING_MAX = 20f;
        private const float HANDLING = 1f;
        private const float ARMOR_MAX = 100f;
        private const float ARMOR = 0f;
        private const float HULL_MAX = 100f;
        private const float HULL = 100f;

        private const float ENGINE_FLIGHT_SPEED = 1f;
        private const float SMALL_ENGINE_FLIGHT_SPEED = 0.5f;
        private const float HANDLING_PERCENT = 0.1f;
        private const float HULL_BONUS = 50f;
        private const float HIT = 30f;
        private const float LETHAL_HIT = 1000f;
        private const string HULL_SOCKET = "hull-plate";

        private GameObject _host;
        private ShipStatEntity _entity;
        private ShipStatSheet _sheet;

        [SetUp]
        public void SetUp() {
            ShipAccumulativeStatsConfiguration configuration =
                AssetDatabase.LoadAssetAtPath<ShipAccumulativeStatsConfiguration>(STATS_CONFIGURATION_PATH);
            DiContainer container = new DiContainer();
            container.Bind<ShipAccumulativeStatsConfiguration>().FromInstance(configuration).AsSingle();
            container.Bind<IStatFactory<ShipStatType>>().To<ShipStatFactory>().AsSingle();
            container.Bind<IStatEntityFactory<ShipStatType>>().To<ShipStatEntityFactory>().AsSingle();
            _host = new GameObject(nameof(ShipStatSheetTests));
            _entity = _host.AddComponent<ShipStatEntity>();
            container.InjectGameObject(_host);
            _sheet = new ShipStatSheet(_entity);
            _sheet.ApplyDefaults(configuration.Defaults);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_host);

        [Test]
        public void WhenDefaultsApplied_ThenStatsMatchTheFormerCodeValues() {
            AssertStat(ShipStatType.Thrust, THRUST_MAX, THRUST);
            AssertStat(ShipStatType.FlightSpeed, FLIGHT_SPEED_MAX, FLIGHT_SPEED);
            AssertStat(ShipStatType.DodgeRange, DODGE_RANGE_MAX, DODGE_RANGE);
            AssertStat(ShipStatType.Handling, HANDLING_MAX, HANDLING);
            AssertStat(ShipStatType.Armor, ARMOR_MAX, ARMOR);
            AssertStat(ShipStatType.Hull, HULL_MAX, HULL);
        }

        [Test]
        public void WhenDefaultsApplied_ThenSnapshotHasFullHull() {
            ShipStatsState state = _sheet.CreateState();

            Assert.AreEqual(HULL, state.Hull, TOLERANCE);
            Assert.AreEqual(HULL_MAX, state.HullMax, TOLERANCE);
        }

        [Test]
        public void WhenHullHitHarderThanItHas_ThenItStopsAtZero() {
            _sheet.Subtract(ShipStatType.Hull, LETHAL_HIT);

            Assert.AreEqual(0f, _sheet.GetFull(ShipStatType.Hull), TOLERANCE);
        }

        [Test]
        public void WhenHullIsEmpty_AndRefilled_ThenItIsAtMax() {
            _sheet.Subtract(ShipStatType.Hull, LETHAL_HIT);

            _sheet.Refill(ShipStatType.Hull);

            Assert.AreEqual(HULL_MAX, _sheet.GetFull(ShipStatType.Hull), TOLERANCE);
        }

        [Test]
        public void WhenHullModuleInstalled_ThenMaxAndCurrentRiseByTheBonus() {
            _sheet.Subtract(ShipStatType.Hull, HIT);

            _sheet.AddModule(HULL_SOCKET, HullModifiers(HULL_BONUS));

            Assert.AreEqual(HULL_MAX + HULL_BONUS, _sheet.GetMax(ShipStatType.Hull), TOLERANCE);
            Assert.AreEqual(HULL - HIT + HULL_BONUS, _sheet.GetFull(ShipStatType.Hull), TOLERANCE);
        }

        [Test]
        public void WhenHullModuleRemovedFromAFullHull_ThenHullIsClampedToTheNewMax() {
            _sheet.AddModule(HULL_SOCKET, HullModifiers(HULL_BONUS));

            Assert.IsTrue(_sheet.RemoveModule(HULL_SOCKET));

            Assert.AreEqual(HULL_MAX, _sheet.GetMax(ShipStatType.Hull), TOLERANCE);
            Assert.AreEqual(HULL_MAX, _sheet.GetFull(ShipStatType.Hull), TOLERANCE);
        }

        [Test]
        public void WhenHullModuleRemovedFromADamagedHull_ThenHullDoesNotHeal() {
            _sheet.AddModule(HULL_SOCKET, HullModifiers(HULL_BONUS));
            _sheet.Subtract(ShipStatType.Hull, HULL_BONUS + HIT);
            float damaged = _sheet.GetFull(ShipStatType.Hull);

            Assert.IsTrue(_sheet.RemoveModule(HULL_SOCKET));

            Assert.AreEqual(damaged, _sheet.GetFull(ShipStatType.Hull), TOLERANCE);
            Assert.LessOrEqual(_sheet.GetFull(ShipStatType.Hull), _sheet.GetMax(ShipStatType.Hull));
        }

        [Test]
        public void WhenEngineInstalled_ThenFinalFlightSpeedAndSnapshotIncludeIt() {
            _sheet.AddModule(LEFT_ENGINE_SOCKET, FlightSpeedModifiers(ENGINE_FLIGHT_SPEED));

            Assert.AreEqual(FLIGHT_SPEED + ENGINE_FLIGHT_SPEED, _sheet.GetFull(ShipStatType.FlightSpeed), TOLERANCE);
            ShipStatsState state = _sheet.CreateState();
            Assert.AreEqual(FLIGHT_SPEED + ENGINE_FLIGHT_SPEED, state.FlightSpeed, TOLERANCE);
            Assert.AreEqual(THRUST, state.Thrust, TOLERANCE);
            Assert.AreEqual(DODGE_RANGE, state.DodgeRange, TOLERANCE);
            Assert.AreEqual(HANDLING, state.Handling, TOLERANCE);
            Assert.AreEqual(ARMOR, state.Armor, TOLERANCE);
        }

        [Test]
        public void WhenTwoEnginesInstalled_AndOneUninstalled_ThenSnapshotKeepsOnlyTheOther() {
            _sheet.AddModule(LEFT_ENGINE_SOCKET, FlightSpeedModifiers(ENGINE_FLIGHT_SPEED));
            _sheet.AddModule(RIGHT_ENGINE_SOCKET, FlightSpeedModifiers(SMALL_ENGINE_FLIGHT_SPEED));
            Assert.AreEqual(
                FLIGHT_SPEED + ENGINE_FLIGHT_SPEED + SMALL_ENGINE_FLIGHT_SPEED,
                _sheet.CreateState().FlightSpeed,
                TOLERANCE);

            Assert.IsTrue(_sheet.RemoveModule(LEFT_ENGINE_SOCKET));

            Assert.AreEqual(FLIGHT_SPEED + SMALL_ENGINE_FLIGHT_SPEED, _sheet.CreateState().FlightSpeed, TOLERANCE);
        }

        [Test]
        public void WhenEngineReinstalledInTheSameSocket_ThenItsSpeedCountsOnce() {
            _sheet.AddModule(LEFT_ENGINE_SOCKET, FlightSpeedModifiers(ENGINE_FLIGHT_SPEED));
            _sheet.AddModule(LEFT_ENGINE_SOCKET, FlightSpeedModifiers(SMALL_ENGINE_FLIGHT_SPEED));

            Assert.AreEqual(FLIGHT_SPEED + SMALL_ENGINE_FLIGHT_SPEED, _sheet.CreateState().FlightSpeed, TOLERANCE);
        }

        [Test]
        public void WhenEmptySocketUninstalled_ThenNothingChanges() {
            Assert.IsFalse(_sheet.RemoveModule(LEFT_ENGINE_SOCKET));

            Assert.AreEqual(FLIGHT_SPEED, _sheet.CreateState().FlightSpeed, TOLERANCE);
        }

        [Test]
        public void WhenModuleWithSeveralModifiersUninstalled_ThenEveryStatIsBackToBase() {
            _sheet.AddModule(LEFT_ENGINE_SOCKET, new[] {
                Modifier(ShipStatType.FlightSpeed, ENGINE_FLIGHT_SPEED, ModifierType.Flat),
                Modifier(ShipStatType.Handling, HANDLING_PERCENT, ModifierType.PercentAdd)
            });
            Assert.AreEqual(FLIGHT_SPEED + ENGINE_FLIGHT_SPEED, _sheet.GetFull(ShipStatType.FlightSpeed), TOLERANCE);
            Assert.AreEqual(HANDLING * (1f + HANDLING_PERCENT), _sheet.GetFull(ShipStatType.Handling), TOLERANCE);

            Assert.IsTrue(_sheet.RemoveModule(LEFT_ENGINE_SOCKET));

            Assert.AreEqual(FLIGHT_SPEED, _sheet.GetFull(ShipStatType.FlightSpeed), TOLERANCE);
            Assert.AreEqual(HANDLING, _sheet.GetFull(ShipStatType.Handling), TOLERANCE);
        }

        [Test]
        public void WhenEngineBroken_ThenItsSpeedIsOff_AndRepairBringsItBack() {
            _sheet.AddModule(LEFT_ENGINE_SOCKET, FlightSpeedModifiers(ENGINE_FLIGHT_SPEED));

            Assert.IsTrue(_sheet.SuspendModule(LEFT_ENGINE_SOCKET), "A working engine was not suspended.");
            Assert.AreEqual(FLIGHT_SPEED, _sheet.GetFull(ShipStatType.FlightSpeed), TOLERANCE, "A broken engine still adds speed.");
            Assert.IsFalse(_sheet.SuspendModule(LEFT_ENGINE_SOCKET), "A broken engine was suspended twice.");

            Assert.IsTrue(_sheet.ResumeModule(LEFT_ENGINE_SOCKET), "A broken engine was not resumed.");
            Assert.AreEqual(FLIGHT_SPEED + ENGINE_FLIGHT_SPEED, _sheet.GetFull(ShipStatType.FlightSpeed), TOLERANCE, "A repaired engine does not add its speed back.");
            Assert.IsFalse(_sheet.ResumeModule(LEFT_ENGINE_SOCKET), "A working engine was resumed again.");
        }

        [Test]
        public void WhenBrokenEngineUninstalled_ThenNothingIsTakenOffTwice_AndReinstallWorks() {
            _sheet.AddModule(LEFT_ENGINE_SOCKET, FlightSpeedModifiers(ENGINE_FLIGHT_SPEED));
            _sheet.AddModule(RIGHT_ENGINE_SOCKET, FlightSpeedModifiers(SMALL_ENGINE_FLIGHT_SPEED));
            _sheet.SuspendModule(LEFT_ENGINE_SOCKET);

            Assert.IsTrue(_sheet.RemoveModule(LEFT_ENGINE_SOCKET), "The broken engine was not removed.");
            Assert.AreEqual(FLIGHT_SPEED + SMALL_ENGINE_FLIGHT_SPEED, _sheet.GetFull(ShipStatType.FlightSpeed), TOLERANCE, "Removing a broken engine took off another module's speed.");

            _sheet.AddModule(LEFT_ENGINE_SOCKET, FlightSpeedModifiers(ENGINE_FLIGHT_SPEED));
            Assert.AreEqual(FLIGHT_SPEED + ENGINE_FLIGHT_SPEED + SMALL_ENGINE_FLIGHT_SPEED, _sheet.GetFull(ShipStatType.FlightSpeed), TOLERANCE, "A reinstalled engine does not work.");
        }

        [Test]
        public void WhenHullDamaged_AndHealed_ThenItRisesUpToTheMaxOnly() {
            _sheet.Subtract(ShipStatType.Hull, HIT);
            _sheet.Add(ShipStatType.Hull, HIT / 2f);
            Assert.AreEqual(HULL - HIT / 2f, _sheet.GetFull(ShipStatType.Hull), TOLERANCE, "Healing did not add the amount.");

            _sheet.Add(ShipStatType.Hull, LETHAL_HIT);
            Assert.AreEqual(HULL_MAX, _sheet.GetFull(ShipStatType.Hull), TOLERANCE, "Healing went past the hull max.");
        }

        private static ModuleDefinition.StatModifierEntry[] FlightSpeedModifiers(float value) =>
            new[] { Modifier(ShipStatType.FlightSpeed, value, ModifierType.Flat) };

        private static ModuleDefinition.StatModifierEntry[] HullModifiers(float value) =>
            new[] { Modifier(ShipStatType.Hull, value, ModifierType.Flat) };

        private static ModuleDefinition.StatModifierEntry Modifier(ShipStatType stat, float value, ModifierType type) =>
            new ModuleDefinition.StatModifierEntry { Stat = stat, Value = value, Type = type };

        private void AssertStat(ShipStatType type, float maxValue, float value) {
            Assert.AreEqual(maxValue, _entity.GetStat(type).MaxValue, TOLERANCE, type + " max");
            Assert.AreEqual(value, _sheet.GetFull(type), TOLERANCE, type + " value");
        }
    }
}
